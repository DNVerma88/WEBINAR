using System.Text.Json;
using System.Text.RegularExpressions;
using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Surveys;
using KnowHub.Application.Models;
using KnowHub.Application.Models.Surveys;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services.Surveys;

public sealed class SurveyService : ISurveyService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly System.Threading.Channels.Channel<Guid> _launchChannel;

    public SurveyService(
        KnowHubDbContext db,
        ICurrentUserAccessor currentUser,
        System.Threading.Channels.Channel<Guid> launchChannel)
    {
        _db = db;
        _currentUser = currentUser;
        _launchChannel = launchChannel;
    }

    public async Task<PagedResult<SurveyDto>> GetSurveysAsync(GetSurveysRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can manage surveys.");

        var query = _db.Surveys
            .Where(s => s.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<SurveyStatus>(request.Status, true, out var statusFilter))
            query = query.Where(s => s.Status == statusFilter);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(s => s.Title.Contains(request.SearchTerm));

        var (data, total) = await query
            .OrderByDescending(s => s.CreatedDate)
            .Select(s => MapToDto(s, new List<SurveyQuestionDto>()))
            .ToPagedListAsync(request.PageNumber, request.PageSize, ct);

        return new PagedResult<SurveyDto>
        {
            Data       = data,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize   = request.PageSize
        };
    }

    public async Task<SurveyDto> GetByIdAsync(Guid surveyId, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can view surveys.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .Include(s => s.Questions.OrderBy(q => q.OrderSequence))
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);
        return MapToDto(survey, MapQuestions(survey.Questions));
    }

    public async Task<SurveyDto> CreateAsync(CreateSurveyRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can create surveys.");

        var survey = new Survey
        {
            TenantId        = _currentUser.TenantId,
            Title           = request.Title.Trim(),
            Description     = request.Description?.Trim(),
            WelcomeMessage  = request.WelcomeMessage?.Trim(),
            ThankYouMessage = request.ThankYouMessage?.Trim(),
            EndsAt          = request.EndsAt,
            IsAnonymous     = request.IsAnonymous,
            Status          = SurveyStatus.Draft,
            CreatedBy       = _currentUser.UserId,
            ModifiedBy      = _currentUser.UserId,
        };

        _db.Surveys.Add(survey);
        await _db.SaveChangesAsync(ct);
        return MapToDto(survey, new List<SurveyQuestionDto>());
    }

    public async Task<SurveyDto> UpdateAsync(Guid surveyId, UpdateSurveyRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can update surveys.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);
        if (survey.Status != SurveyStatus.Draft)
            throw new BusinessRuleException("Cannot edit a launched survey.");
        if (survey.RecordVersion != request.RecordVersion)
            throw new ConflictException("The survey has been modified by another user. Please refresh and try again.");

        survey.Title           = request.Title.Trim();
        survey.Description     = request.Description?.Trim();
        survey.WelcomeMessage  = request.WelcomeMessage?.Trim();
        survey.ThankYouMessage = request.ThankYouMessage?.Trim();
        survey.EndsAt          = request.EndsAt;
        survey.ModifiedBy      = _currentUser.UserId;
        survey.ModifiedOn      = DateTime.UtcNow;
        survey.RecordVersion++;

        await _db.SaveChangesAsync(ct);
        return MapToDto(survey, new List<SurveyQuestionDto>());
    }

    public async Task DeleteAsync(Guid surveyId, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can delete surveys.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);

        // SurveyResponses and SurveyAnswers use Restrict FK — delete them explicitly first.
        var responseIds = await _db.SurveyResponses
            .Where(r => r.SurveyId == surveyId)
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (responseIds.Count > 0)
        {
            await _db.SurveyAnswers
                .Where(a => responseIds.Contains(a.ResponseId))
                .ExecuteDeleteAsync(ct);

            await _db.SurveyResponses
                .Where(r => r.SurveyId == surveyId)
                .ExecuteDeleteAsync(ct);
        }

        _db.Surveys.Remove(survey);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<SurveyDto> CopyAsync(Guid surveyId, CopySurveyRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can copy surveys.");

        var source = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .Include(s => s.Questions.OrderBy(q => q.OrderSequence))
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (source is null) throw new NotFoundException("Survey", surveyId);

        var excludeSet = request.ExcludeQuestionIds.ToHashSet();
        var rawTitle   = request.NewTitle?.Trim();
        var newTitle   = string.IsNullOrWhiteSpace(rawTitle)
            ? $"Copy of {source.Title}"
            : rawTitle;
        if (newTitle.Length > 300) newTitle = newTitle[..300];

        var copy = new Survey
        {
            TenantId        = _currentUser.TenantId,
            Title           = newTitle,
            Description     = source.Description,
            WelcomeMessage  = source.WelcomeMessage,
            ThankYouMessage = source.ThankYouMessage,
            TokenExpiryDays = source.TokenExpiryDays,
            IsAnonymous     = source.IsAnonymous,
            Status          = SurveyStatus.Draft,
            CreatedBy       = _currentUser.UserId,
            ModifiedBy      = _currentUser.UserId,
        };

        int order = 0;
        foreach (var q in source.Questions.Where(q => !excludeSet.Contains(q.Id)))
        {
            copy.Questions.Add(new SurveyQuestion
            {
                TenantId      = _currentUser.TenantId,
                QuestionText  = q.QuestionText,
                QuestionType  = q.QuestionType,
                OptionsJson   = q.OptionsJson,
                MinRating     = q.MinRating,
                MaxRating     = q.MaxRating,
                IsRequired    = q.IsRequired,
                OrderSequence = order++,
                CreatedBy     = _currentUser.UserId,
                ModifiedBy    = _currentUser.UserId,
            });
        }

        _db.Surveys.Add(copy);
        await _db.SaveChangesAsync(ct);
        return MapToDto(copy, MapQuestions(copy.Questions));
    }

    public async Task<SurveyDto> LaunchAsync(Guid surveyId, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can launch surveys.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);
        if (survey.Status != SurveyStatus.Draft)
            throw new BusinessRuleException("Only Draft surveys can be launched.");
        if (!survey.Questions.Any())
            throw new BusinessRuleException("A survey must have at least one question before it can be launched.");

        var employeeCount = await _db.Users
            .CountAsync(u => u.TenantId == _currentUser.TenantId
                          && u.IsActive, ct);

        survey.Status      = SurveyStatus.Active;
        survey.LaunchedAt  = DateTime.UtcNow;
        survey.TotalInvited = employeeCount;
        survey.ModifiedBy  = _currentUser.UserId;
        survey.ModifiedOn  = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // Queue the launch job (non-blocking)
        await _launchChannel.Writer.WriteAsync(survey.Id, ct);

        return MapToDto(survey, MapQuestions(survey.Questions));
    }

    public async Task<SurveyDto> CloseAsync(Guid surveyId, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can close surveys.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);
        if (survey.Status != SurveyStatus.Active)
            throw new BusinessRuleException("Only Active surveys can be closed.");

        survey.Status     = SurveyStatus.Closed;
        survey.ClosedAt   = DateTime.UtcNow;
        survey.ModifiedBy = _currentUser.UserId;
        survey.ModifiedOn = DateTime.UtcNow;

        // Expire all remaining Sent invitations
        await _db.SurveyInvitations
            .Where(i => i.SurveyId == surveyId && i.Status == SurveyInvitationStatus.Sent)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.Status, SurveyInvitationStatus.Expired)
                .SetProperty(i => i.ModifiedOn, DateTime.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
        return MapToDto(survey, new List<SurveyQuestionDto>());
    }

    public async Task<SurveyResultsDto> GetResultsAsync(Guid surveyId, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can view survey results.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .Include(s => s.Questions.OrderBy(q => q.OrderSequence))
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);

        var answers = await _db.SurveyAnswers
            .Where(a => a.Response.SurveyId == surveyId)
            .AsNoTracking()
            .ToListAsync(ct);

        var questionResults = new List<QuestionResultDto>();
        foreach (var question in survey.Questions)
        {
            var qAnswers = answers.Where(a => a.QuestionId == question.Id).ToList();
            questionResults.Add(BuildQuestionResult(question, qAnswers));
        }

        var rate = survey.TotalInvited > 0
            ? (int)Math.Round((double)survey.TotalResponded / survey.TotalInvited * 100)
            : 0;

        return new SurveyResultsDto(
            survey.Id,
            survey.Title,
            survey.IsAnonymous,
            survey.TotalInvited,
            survey.TotalResponded,
            rate,
            questionResults);
    }

    // -- Question management ------------------------------------------------

    public async Task<SurveyQuestionDto> AddQuestionAsync(Guid surveyId, AddSurveyQuestionRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can add questions.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);
        if (survey.Status != SurveyStatus.Draft)
            throw new BusinessRuleException("Questions can only be added while the survey is in Draft status.");

        var question = new SurveyQuestion
        {
            TenantId      = _currentUser.TenantId,
            SurveyId      = surveyId,
            QuestionText  = StripHtml(request.QuestionText),
            QuestionType  = request.QuestionType,
            OptionsJson   = SerializeOptions(request.Options),
            MinRating     = request.MinRating,
            MaxRating     = request.MaxRating,
            IsRequired    = request.IsRequired,
            OrderSequence = request.OrderSequence,
            CreatedBy     = _currentUser.UserId,
            ModifiedBy    = _currentUser.UserId,
        };

        _db.SurveyQuestions.Add(question);
        await _db.SaveChangesAsync(ct);
        return MapQuestionToDto(question);
    }

    public async Task<SurveyQuestionDto> UpdateQuestionAsync(Guid surveyId, Guid questionId, UpdateSurveyQuestionRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can update questions.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);
        if (survey.Status != SurveyStatus.Draft)
            throw new BusinessRuleException("Questions can only be modified while the survey is in Draft status.");

        var question = await _db.SurveyQuestions
            .Where(q => q.Id == questionId && q.SurveyId == surveyId)
            .FirstOrDefaultAsync(ct);

        if (question is null) throw new NotFoundException("SurveyQuestion", questionId);
        if (question.RecordVersion != request.RecordVersion)
            throw new ConflictException("The question has been modified. Please refresh and try again.");

        question.QuestionText  = StripHtml(request.QuestionText);
        question.QuestionType  = request.QuestionType;
        question.OptionsJson   = SerializeOptions(request.Options);
        question.MinRating     = request.MinRating;
        question.MaxRating     = request.MaxRating;
        question.IsRequired    = request.IsRequired;
        question.OrderSequence = request.OrderSequence;
        question.ModifiedBy    = _currentUser.UserId;
        question.ModifiedOn    = DateTime.UtcNow;
        question.RecordVersion++;

        await _db.SaveChangesAsync(ct);
        return MapQuestionToDto(question);
    }

    public async Task DeleteQuestionAsync(Guid surveyId, Guid questionId, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can delete questions.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);

        // Guard 1: survey must be Draft
        if (survey.Status != SurveyStatus.Draft)
            throw new BusinessRuleException("Questions cannot be deleted after the survey has been launched.");

        var question = await _db.SurveyQuestions
            .Where(q => q.Id == questionId && q.SurveyId == surveyId)
            .FirstOrDefaultAsync(ct);

        if (question is null) throw new NotFoundException("SurveyQuestion", questionId);

        // Guard 2: no answers reference this question
        if (await _db.SurveyAnswers.AnyAsync(a => a.QuestionId == questionId, ct))
            throw new ConflictException("This question has recorded answers and cannot be deleted. Use \"Copy Survey\" to create a new survey and exclude this question.");

        _db.SurveyQuestions.Remove(question);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReorderQuestionsAsync(Guid surveyId, ReorderQuestionsRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can reorder questions.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);
        if (survey.Status != SurveyStatus.Draft)
            throw new BusinessRuleException("Questions can only be reordered while the survey is in Draft status.");

        var questions = await _db.SurveyQuestions
            .Where(q => q.SurveyId == surveyId)
            .ToListAsync(ct);

        var questionMap = questions.ToDictionary(q => q.Id);
        for (int i = 0; i < request.Ordered.Count; i++)
        {
            if (questionMap.TryGetValue(request.Ordered[i], out var q))
            {
                q.OrderSequence = i;
                q.ModifiedBy    = _currentUser.UserId;
                q.ModifiedOn    = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    // -- Mapping helpers ----------------------------------------------------

    private static SurveyDto MapToDto(Survey s, IReadOnlyList<SurveyQuestionDto> questions)
    {
        var rate = s.TotalInvited > 0
            ? (int)Math.Round((double)s.TotalResponded / s.TotalInvited * 100)
            : 0;
        return new SurveyDto(
            s.Id, s.TenantId, s.Title, s.Description, s.WelcomeMessage, s.ThankYouMessage,
            s.Status.ToString(), s.EndsAt, s.IsAnonymous,
            s.LaunchedAt, s.ClosedAt, s.TotalInvited, s.TotalResponded, rate,
            questions.ToList(), s.CreatedDate, s.CreatedBy);
    }

    private static List<SurveyQuestionDto> MapQuestions(IEnumerable<SurveyQuestion> questions)
        => questions.Select(SurveyMappings.ToDto).ToList();

    private static SurveyQuestionDto MapQuestionToDto(SurveyQuestion q)
        => SurveyMappings.ToDto(q);

    private static QuestionResultDto BuildQuestionResult(SurveyQuestion question, List<SurveyAnswer> answers)
    {
        int totalAnswers = answers.Count;

        List<OptionCountDto>? optionCounts     = null;
        double?               averageRating    = null;
        int?                  minRatingGiven   = null;
        int?                  maxRatingGiven   = null;
        List<string>?         textAnswers      = null;

        switch (question.QuestionType)
        {
            case SurveyQuestionType.Text:
                textAnswers = answers
                    .Where(a => a.AnswerText is not null)
                    .Select(a => a.AnswerText!)
                    .ToList();
                break;

            case SurveyQuestionType.Rating:
                var ratings = answers
                    .Where(a => a.RatingValue.HasValue)
                    .Select(a => a.RatingValue!.Value)
                    .ToList();
                if (ratings.Count > 0)
                {
                    averageRating  = ratings.Average();
                    minRatingGiven = ratings.Min();
                    maxRatingGiven = ratings.Max();
                    optionCounts   = ratings
                        .GroupBy(v => v)
                        .OrderBy(g => g.Key)
                        .Select(g => new OptionCountDto(
                            g.Key.ToString(),
                            g.Count(),
                            totalAnswers > 0 ? Math.Round((double)g.Count() / totalAnswers * 100, 1) : 0))
                        .ToList();
                }
                break;

            case SurveyQuestionType.YesNo:
            case SurveyQuestionType.SingleChoice:
                optionCounts = answers
                    .Where(a => a.AnswerText is not null)
                    .GroupBy(a => a.AnswerText!)
                    .Select(g => new OptionCountDto(
                        g.Key, g.Count(),
                        totalAnswers > 0 ? Math.Round((double)g.Count() / totalAnswers * 100, 1) : 0))
                    .ToList();
                break;

            case SurveyQuestionType.MultipleChoice:
                var allOptions = answers
                    .Where(a => a.AnswerOptionsJson is not null)
                    .SelectMany(a => JsonSerializer.Deserialize<List<string>>(a.AnswerOptionsJson!) ?? new List<string>())
                    .ToList();
                int respondentCount = answers.Count(a => a.AnswerOptionsJson is not null);
                optionCounts = allOptions
                    .GroupBy(o => o)
                    .Select(g => new OptionCountDto(
                        g.Key, g.Count(),
                        respondentCount > 0 ? Math.Round((double)g.Count() / respondentCount * 100, 1) : 0))
                    .ToList();
                break;
        }

        return new QuestionResultDto(
            question.Id, question.QuestionText, question.QuestionType.ToString(),
            totalAnswers, optionCounts, averageRating, minRatingGiven, maxRatingGiven, textAnswers);
    }

    private static string StripHtml(string input)
        => Regex.Replace(input, "<[^>]*>", string.Empty).Trim();

    private static string? SerializeOptions(List<string>? options)
    {
        if (options is null || options.Count == 0) return null;
        var sanitised = options.Select(o => StripHtml(o)).ToList();
        return JsonSerializer.Serialize(sanitised);
    }
}
