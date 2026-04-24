using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
using Microsoft.Extensions.Caching.Memory;

namespace KnowHub.Infrastructure.Services.Surveys;

public sealed class SurveyResponseService : ISurveyResponseService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IMemoryCache _cache;

    public SurveyResponseService(
        KnowHubDbContext db,
        ICurrentUserAccessor currentUser,
        IMemoryCache cache)
    {
        _db          = db;
        _currentUser = currentUser;
        _cache       = cache;
    }

    public async Task<SurveyFormDto> GetFormByTokenAsync(string plainToken, CancellationToken ct)
    {
        var tokenHash   = HashToken(plainToken);
        var now         = DateTime.UtcNow;

        var invitation = await _db.SurveyInvitations
            .Where(i => i.TokenHash == tokenHash)
            .Include(i => i.Survey)
                .ThenInclude(s => s.Questions.OrderBy(q => q.OrderSequence))
            .FirstOrDefaultAsync(ct);

        // Use a generic error message to avoid token existence leakage
        if (invitation is null)
            throw new NotFoundException("Survey not found or link is invalid.");

        ValidateInvitationForAccess(invitation, now);

        // Record first access time for funnel analytics (set once, never overwrite)
        if (invitation.TokenAccessedAt is null)
        {
            invitation.TokenAccessedAt = now;
            invitation.ModifiedOn      = now;
            await _db.SaveChangesAsync(ct);

            // Evict analytics caches for this survey
            EvictSurveyCache(invitation.SurveyId);
        }

        var survey    = invitation.Survey;
        var questions = survey.Questions
            .Select(q => MapQuestionToDto(q))
            .ToList();

        return new SurveyFormDto(
            survey.Id, survey.Title, survey.WelcomeMessage, survey.ThankYouMessage,
            questions, invitation.ExpiresAt ?? now.AddDays(7));
    }

    public async Task<SurveyResponseDto> SubmitAsync(string plainToken, SubmitSurveyRequest request, CancellationToken ct)
    {
        var tokenHash = HashToken(plainToken);
        var now       = DateTime.UtcNow;

        var invitation = await _db.SurveyInvitations
            .Where(i => i.TokenHash == tokenHash)
            .Include(i => i.Survey)
                .ThenInclude(s => s.Questions)
            .FirstOrDefaultAsync(ct);

        if (invitation is null)
            throw new NotFoundException("Survey not found or link is invalid.");

        ValidateInvitationForAccess(invitation, now);

        var survey    = invitation.Survey;
        var questions = survey.Questions.ToList();

        // Validate required questions are answered
        var missingRequired = questions
            .Where(q => q.IsRequired)
            .Where(q => !request.Answers.Any(a => a.QuestionId == q.Id))
            .Select(q => q.Id)
            .ToList();

        if (missingRequired.Any())
            throw new KnowHub.Domain.Exceptions.ValidationException(
                "Answers", $"Missing answers for required questions: {string.Join(", ", missingRequired)}");

        // Validate each answer value
        foreach (var answer in request.Answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question is null) continue;
            ValidateAnswer(question, answer);
        }

        // Double-submission guard
        var alreadySubmitted = await _db.SurveyResponses
            .AnyAsync(r => r.TenantId == invitation.TenantId
                        && r.SurveyId == invitation.SurveyId
                        && r.UserId   == invitation.UserId, ct);

        if (alreadySubmitted)
            throw new ConflictException("You have already submitted this survey.");

        // Create response and answers
        var response = new SurveyResponse
        {
            TenantId     = invitation.TenantId,
            SurveyId     = invitation.SurveyId,
            UserId       = invitation.UserId,
            InvitationId = invitation.Id,
            SubmittedAt  = now,
            CreatedBy    = invitation.UserId,
            ModifiedBy   = invitation.UserId,
        };

        foreach (var answerReq in request.Answers)
        {
            response.Answers.Add(new SurveyAnswer
            {
                TenantId        = invitation.TenantId,
                QuestionId      = answerReq.QuestionId,
                AnswerText      = answerReq.AnswerText,
                AnswerOptionsJson = answerReq.AnswerOptions is not null
                    ? JsonSerializer.Serialize(answerReq.AnswerOptions)
                    : null,
                RatingValue     = answerReq.RatingValue,
                CreatedBy       = invitation.UserId,
                ModifiedBy      = invitation.UserId,
            });
        }

        _db.SurveyResponses.Add(response);

        // Update invitation status
        invitation.Status      = SurveyInvitationStatus.Submitted;
        invitation.SubmittedAt = now;
        invitation.ModifiedOn  = now;

        // Increment survey TotalResponded counter
        await _db.Surveys
            .Where(s => s.Id == invitation.SurveyId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.TotalResponded, x => x.TotalResponded + 1)
                .SetProperty(x => x.ModifiedOn, now), ct);

        await _db.SaveChangesAsync(ct);

        // Evict analytics caches
        EvictSurveyCache(invitation.SurveyId);

        return new SurveyResponseDto(
            response.Id,
            response.SurveyId,
            survey.IsAnonymous ? null : response.UserId,
            null,
            response.SubmittedAt);
    }

    public async Task<PagedResult<SurveyResponseDto>> GetResponsesAsync(
        Guid surveyId, GetSurveyResponsesRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can view survey responses.");

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);

        var query = _db.SurveyResponses
            .Where(r => r.SurveyId == surveyId && r.TenantId == _currentUser.TenantId)
            .Include(r => r.User)
            .AsNoTracking();

        var (data, total) = await query
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => new SurveyResponseDto(
                r.Id,
                r.SurveyId,
                survey.IsAnonymous ? (Guid?)null : r.UserId,
                survey.IsAnonymous ? null : r.User.FullName,
                r.SubmittedAt))
            .ToPagedListAsync(request.PageNumber, request.PageSize, ct);

        return new PagedResult<SurveyResponseDto>
        {
            Data       = data,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize   = request.PageSize
        };
    }

    // -- Private helpers ----------------------------------------------------

    private static string HashToken(string plainToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void ValidateInvitationForAccess(SurveyInvitation invitation, DateTime now)
    {
        if (invitation.Status == SurveyInvitationStatus.Submitted)
            throw new BusinessRuleException("You have already submitted this survey.");

        if (invitation.Status == SurveyInvitationStatus.Expired || invitation.ExpiresAt < now)
            throw new BusinessRuleException("This survey link has expired. Please contact your administrator to request a new link.");

        if (invitation.Status == SurveyInvitationStatus.Failed)
            throw new BusinessRuleException("This invitation link is not active.");

        if (invitation.Survey.Status != SurveyStatus.Active)
            throw new BusinessRuleException("This survey is no longer accepting responses.");
    }

    private static void ValidateAnswer(SurveyQuestion question, SurveyAnswerRequest answer)
    {
        switch (question.QuestionType)
        {
            case SurveyQuestionType.YesNo:
                if (answer.AnswerText is not null &&
                    !string.Equals(answer.AnswerText, "Yes", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(answer.AnswerText, "No", StringComparison.OrdinalIgnoreCase))
                    throw new KnowHub.Domain.Exceptions.ValidationException("AnswerText", $"Answer for question {question.Id} must be 'Yes' or 'No'.");
                break;

            case SurveyQuestionType.SingleChoice:
                if (answer.AnswerText is not null && question.OptionsJson is not null)
                {
                    var options = JsonSerializer.Deserialize<List<string>>(question.OptionsJson) ?? new List<string>();
                    if (!options.Contains(answer.AnswerText, StringComparer.Ordinal))
                        throw new KnowHub.Domain.Exceptions.ValidationException("AnswerText", $"Invalid option for question {question.Id}.");
                }
                break;

            case SurveyQuestionType.MultipleChoice:
                if (answer.AnswerOptions is not null && question.OptionsJson is not null)
                {
                    var options = JsonSerializer.Deserialize<List<string>>(question.OptionsJson) ?? new List<string>();
                    if (answer.AnswerOptions.Count == 0)
                        throw new KnowHub.Domain.Exceptions.ValidationException("AnswerOptions", $"At least one option must be selected for question {question.Id}.");
                    var invalidOptions = answer.AnswerOptions.Except(options).ToList();
                    if (invalidOptions.Any())
                        throw new KnowHub.Domain.Exceptions.ValidationException("AnswerOptions", $"Invalid options for question {question.Id}: {string.Join(", ", invalidOptions)}");
                }
                break;

            case SurveyQuestionType.Rating:
                if (answer.RatingValue.HasValue)
                {
                    if (answer.RatingValue.Value < question.MinRating || answer.RatingValue.Value > question.MaxRating)
                        throw new KnowHub.Domain.Exceptions.ValidationException(
                            "RatingValue", $"Rating for question {question.Id} must be between {question.MinRating} and {question.MaxRating}.");
                }
                break;
        }
    }

    private static SurveyQuestionDto MapQuestionToDto(SurveyQuestion q)
        => SurveyMappings.ToDto(q);

    private void EvictSurveyCache(Guid surveyId)
    {
        _cache.Remove($"survey-analytics-dashboard-{surveyId}");
        _cache.Remove($"survey-analytics-questions-{surveyId}");
    }
}
