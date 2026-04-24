using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Surveys;
using KnowHub.Application.Models.Surveys.Analytics;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KnowHub.Infrastructure.Services.Surveys;

public sealed class SurveyAnalyticsService : ISurveyAnalyticsService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SurveyAnalyticsService> _logger;

    public SurveyAnalyticsService(
        KnowHubDbContext db,
        ICurrentUserAccessor currentUser,
        IMemoryCache cache,
        ILogger<SurveyAnalyticsService> logger)
    {
        _db          = db;
        _currentUser = currentUser;
        _cache       = cache;
        _logger      = logger;
    }

    public async Task<SurveyAnalyticsSummaryDto> GetDashboardAsync(Guid surveyId, CancellationToken ct = default)
    {
        var cacheKey = $"survey-analytics-dashboard-{surveyId}";
        if (_cache.TryGetValue(cacheKey, out SurveyAnalyticsSummaryDto? cached) && cached is not null)
            return cached;

        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);

        var avgSeconds = await _db.SurveyResponses
            .Where(r => r.SurveyId == surveyId)
            .Join(_db.SurveyInvitations,
                r => r.InvitationId,
                i => i.Id,
                (r, i) => new { r.SubmittedAt, i.TokenAccessedAt })
            .Where(x => x.TokenAccessedAt != null)
            .Select(x => (double)(x.SubmittedAt - x.TokenAccessedAt!.Value).TotalSeconds)
            .ToListAsync(ct);

        var avgCompletionSeconds = avgSeconds.Count > 0 ? avgSeconds.Average() : 0;
        var responsePct          = survey.TotalInvited > 0
            ? Math.Round((double)survey.TotalResponded / survey.TotalInvited * 100, 1)
            : 0;
        var health = responsePct >= 70 ? "Healthy"
                   : responsePct >= 40 ? "AtRisk"
                   : "LowEngagement";

        var result = new SurveyAnalyticsSummaryDto(
            survey.Id, survey.Title, survey.TotalInvited, survey.TotalResponded,
            responsePct, avgCompletionSeconds, health);

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<IReadOnlyList<SurveyQuestionAnalyticsDto>> GetQuestionStatsAsync(
        Guid surveyId, string? departmentFilter, string? roleFilter,
        DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .Include(s => s.Questions.OrderBy(q => q.OrderSequence))
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);

        var answersQuery = _db.SurveyAnswers
            .Where(a => a.Response.SurveyId == surveyId)
            .Join(_db.SurveyResponses,
                a => a.ResponseId,
                r => r.Id,
                (a, r) => new { Answer = a, Response = r })
            .Join(_db.Users,
                x => x.Response.UserId,
                u => u.Id,
                (x, u) => new { x.Answer, x.Response, User = u });

        if (!string.IsNullOrWhiteSpace(departmentFilter))
            answersQuery = answersQuery.Where(x => x.User.Department == departmentFilter);

        if (fromDate.HasValue)
            answersQuery = answersQuery.Where(x => x.Response.SubmittedAt >= fromDate.Value);

        if (toDate.HasValue)
            answersQuery = answersQuery.Where(x => x.Response.SubmittedAt <= toDate.Value);

        var allAnswers = await answersQuery
            .Select(x => new
            {
                x.Answer.QuestionId,
                x.Answer.AnswerText,
                x.Answer.AnswerOptionsJson,
                x.Answer.RatingValue
            })
            .ToListAsync(ct);

        var results = new List<SurveyQuestionAnalyticsDto>();
        foreach (var question in survey.Questions)
        {
            var qAnswers = allAnswers.Where(a => a.QuestionId == question.Id).ToList();
            int total    = qAnswers.Count;

            List<OptionStatDto>  optionStats  = new();
            double?              avgRating    = null;
            int?                 minRating    = null;
            int?                 maxRating    = null;
            List<string>         textAnswers  = new();

            switch (question.QuestionType)
            {
                case SurveyQuestionType.Text:
                    textAnswers = qAnswers
                        .Where(a => a.AnswerText is not null)
                        .Select(a => a.AnswerText!)
                        .Take(200)
                        .ToList();
                    break;

                case SurveyQuestionType.Rating:
                    var ratings = qAnswers
                        .Where(a => a.RatingValue.HasValue)
                        .Select(a => a.RatingValue!.Value)
                        .ToList();
                    if (ratings.Count > 0)
                    {
                        avgRating = ratings.Average();
                        minRating = ratings.Min();
                        maxRating = ratings.Max();
                        optionStats = ratings
                            .GroupBy(v => v)
                            .OrderBy(g => g.Key)
                            .Select(g => new OptionStatDto(
                                g.Key.ToString(), g.Count(),
                                total > 0 ? Math.Round((double)g.Count() / total * 100, 1) : 0))
                            .ToList();
                    }
                    break;

                case SurveyQuestionType.SingleChoice:
                case SurveyQuestionType.YesNo:
                    optionStats = qAnswers
                        .Where(a => a.AnswerText is not null)
                        .GroupBy(a => a.AnswerText!)
                        .Select(g => new OptionStatDto(
                            g.Key, g.Count(),
                            total > 0 ? Math.Round((double)g.Count() / total * 100, 1) : 0))
                        .ToList();
                    break;

                case SurveyQuestionType.MultipleChoice:
                    var allOpts = qAnswers
                        .Where(a => a.AnswerOptionsJson is not null)
                        .SelectMany(a =>
                        {
                            try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(a.AnswerOptionsJson!) ?? new List<string>(); }
                            catch { return new List<string>(); }
                        })
                        .ToList();
                    int respondents = qAnswers.Count(a => a.AnswerOptionsJson is not null);
                    optionStats = allOpts
                        .GroupBy(o => o)
                        .Select(g => new OptionStatDto(
                            g.Key, g.Count(),
                            respondents > 0 ? Math.Round((double)g.Count() / respondents * 100, 1) : 0))
                        .ToList();
                    break;
            }

            results.Add(new SurveyQuestionAnalyticsDto(
                question.Id, question.QuestionText, question.QuestionType,
                total, optionStats, avgRating, minRating, maxRating, textAnswers));
        }

        return results;
    }

    public async Task<SurveyDepartmentBreakdownDto> GetDepartmentBreakdownAsync(
        Guid surveyId, Guid questionId, CancellationToken ct = default)
    {
        var question = await _db.SurveyQuestions
            .Where(q => q.Id == questionId && q.SurveyId == surveyId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (question is null) throw new NotFoundException("SurveyQuestion", questionId);

        var rows = await _db.SurveyAnswers
            .Where(a => a.QuestionId == questionId && a.RatingValue != null)
            .Join(_db.SurveyResponses, a => a.ResponseId, r => r.Id, (a, r) => new { a.RatingValue, r.UserId })
            .Join(_db.Users, x => x.UserId, u => u.Id, (x, u) => new { x.RatingValue, u.Department })
            .Where(x => x.Department != null)
            .GroupBy(x => x.Department!)
            .Select(g => new DepartmentRowDto(
                g.Key,
                g.Average(x => (double)x.RatingValue!.Value),
                g.Count()))
            .ToListAsync(ct);

        return new SurveyDepartmentBreakdownDto(questionId, question.QuestionText, rows);
    }

    public async Task<SurveyNpsReportDto> GetNpsReportAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);

        var npsQuestion = await _db.SurveyQuestions
            .Where(q => q.SurveyId == surveyId
                     && q.QuestionType == SurveyQuestionType.Rating
                     && q.MinRating == 0 && q.MaxRating == 10)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (npsQuestion is null)
            throw new BusinessRuleException("This survey has no NPS (0–10 rating) question.");

        var ratings = await _db.SurveyAnswers
            .Where(a => a.QuestionId == npsQuestion.Id && a.RatingValue != null)
            .Select(a => a.RatingValue!.Value)
            .ToListAsync(ct);

        int total      = ratings.Count;
        int promoters  = ratings.Count(r => r >= 9);
        int passives   = ratings.Count(r => r is >= 7 and <= 8);
        int detractors = ratings.Count(r => r <= 6);
        int npsScore   = total > 0
            ? (int)Math.Round((promoters - detractors) / (double)total * 100)
            : 0;

        return new SurveyNpsReportDto(
            survey.Id, survey.Title,
            promoters, passives, detractors, npsScore,
            total > 0 ? Math.Round((double)promoters / total * 100, 1) : 0,
            total > 0 ? Math.Round((double)passives / total * 100, 1) : 0,
            total > 0 ? Math.Round((double)detractors / total * 100, 1) : 0);
    }

    public async Task<SurveyNpsTrendDto> GetNpsTrendAsync(IReadOnlyList<Guid> surveyIds, CancellationToken ct = default)
    {
        var cappedIds = surveyIds.Take(10).ToList();

        var surveys = await _db.Surveys
            .Where(s => cappedIds.Contains(s.Id) && s.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .ToListAsync(ct);

        var dataPoints = new List<NpsTrendPointDto>();
        foreach (var survey in surveys.OrderBy(s => s.LaunchedAt))
        {
            try
            {
                var dto = await GetNpsReportAsync(survey.Id, ct);
                dataPoints.Add(new NpsTrendPointDto(
                    survey.Id, survey.Title,
                    survey.LaunchedAt ?? survey.CreatedDate,
                    dto.NpsScore));
            }
            catch (BusinessRuleException)
            {
                // Survey has no NPS question — skip
            }
        }

        return new SurveyNpsTrendDto(dataPoints);
    }

    public async Task<SurveyParticipationFunnelDto> GetParticipationFunnelAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);

        var invited   = survey.TotalInvited;
        var sent      = await _db.SurveyInvitations
            .CountAsync(i => i.SurveyId == surveyId && i.Status != SurveyInvitationStatus.Pending, ct);
        var accessed  = await _db.SurveyInvitations
            .CountAsync(i => i.SurveyId == surveyId && i.TokenAccessedAt != null, ct);
        var submitted = survey.TotalResponded;

        var submissionRate = invited > 0
            ? Math.Round((double)submitted / invited * 100, 1) : 0;
        var startToSubmit  = accessed > 0
            ? Math.Round((double)submitted / accessed * 100, 1) : 0;

        return new SurveyParticipationFunnelDto(
            invited, sent, accessed, submitted, submissionRate, startToSubmit);
    }

    public async Task<SurveyHeatmapDto> GetHeatmapAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await _db.Surveys
            .Where(s => s.Id == surveyId && s.TenantId == _currentUser.TenantId)
            .Include(s => s.Questions.Where(q => q.QuestionType == SurveyQuestionType.Rating)
                                      .OrderBy(q => q.OrderSequence))
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", surveyId);

        var ratingQuestions = survey.Questions
            .Where(q => q.QuestionType == SurveyQuestionType.Rating)
            .Take(10)
            .ToList();

        if (!ratingQuestions.Any())
            return new SurveyHeatmapDto(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<double[]>());

        var questionIds = ratingQuestions.Select(q => q.Id).ToList();

        var data = await _db.SurveyAnswers
            .Where(a => questionIds.Contains(a.QuestionId) && a.RatingValue != null)
            .Join(_db.SurveyResponses, a => a.ResponseId, r => r.Id, (a, r) => new { a.QuestionId, a.RatingValue, r.UserId })
            .Join(_db.Users, x => x.UserId, u => u.Id, (x, u) => new { x.QuestionId, x.RatingValue, u.Department })
            .Where(x => x.Department != null)
            .ToListAsync(ct);

        var departments = data.Select(x => x.Department!).Distinct().OrderBy(d => d).Take(20).ToList();
        var matrix      = new double[departments.Count][];

        for (int di = 0; di < departments.Count; di++)
        {
            matrix[di] = new double[ratingQuestions.Count];
            for (int qi = 0; qi < ratingQuestions.Count; qi++)
            {
                var cell = data
                    .Where(x => x.Department == departments[di] && x.QuestionId == ratingQuestions[qi].Id)
                    .ToList();
                matrix[di][qi] = cell.Count > 0
                    ? cell.Average(x => (double)x.RatingValue!.Value)
                    : double.NaN;
            }
        }

        return new SurveyHeatmapDto(
            ratingQuestions.Select(q => q.QuestionText).ToList(),
            departments,
            matrix);
    }

    public async Task<SurveyComparisonDto> CompareSurveysAsync(Guid surveyIdA, Guid surveyIdB, CancellationToken ct = default)
    {
        var surveys = await _db.Surveys
            .Where(s => (s.Id == surveyIdA || s.Id == surveyIdB) && s.TenantId == _currentUser.TenantId)
            .Include(s => s.Questions)
            .AsNoTracking()
            .ToListAsync(ct);

        var summaries = surveys.Select(s =>
        {
            double rate = s.TotalInvited > 0
                ? Math.Round((double)s.TotalResponded / s.TotalInvited * 100, 1) : 0;
            return new SurveyCompSummaryDto(s.Id, s.Title, s.LaunchedAt, rate);
        }).ToList();

        var questionsA = surveys.FirstOrDefault(s => s.Id == surveyIdA)?.Questions
            .Select(q => q.QuestionText.Trim().ToLowerInvariant()) ?? Enumerable.Empty<string>();
        var questionsB = surveys.FirstOrDefault(s => s.Id == surveyIdB)?.Questions
            .Select(q => q.QuestionText.Trim().ToLowerInvariant()) ?? Enumerable.Empty<string>();

        var sharedTexts = questionsA.Intersect(questionsB).ToList();

        var sharedQuestions = new List<SharedQuestionCompDto>();
        foreach (var text in sharedTexts)
        {
            var statsA = await GetQuestionStatsForTextAsync(surveyIdA, text, ct);
            var statsB = await GetQuestionStatsForTextAsync(surveyIdB, text, ct);
            sharedQuestions.Add(new SharedQuestionCompDto(text, new[] { statsA, statsB }.OfType<SurveyQuestionAnalyticsDto>().ToList()));
        }

        return new SurveyComparisonDto(summaries, sharedQuestions);
    }

    public async Task<(byte[] Data, string FileName)> ExportToCsvAsync(SurveyExportRequest request, CancellationToken ct = default)
    {
        var survey = await _db.Surveys
            .Where(s => s.Id == request.SurveyId && s.TenantId == _currentUser.TenantId)
            .Include(s => s.Questions.OrderBy(q => q.OrderSequence))
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", request.SurveyId);

        bool includeRespondentInfo = !survey.IsAnonymous && request.IncludeRespondentInfo;

        var responsesQuery = _db.SurveyResponses
            .Where(r => r.SurveyId == request.SurveyId)
            .Include(r => r.Answers)
            .Include(r => r.User)
            .AsNoTracking();

        if (request.FromDate.HasValue) responsesQuery = responsesQuery.Where(r => r.SubmittedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)   responsesQuery = responsesQuery.Where(r => r.SubmittedAt <= request.ToDate.Value);

        var responses = await responsesQuery.ToListAsync(ct);

        using var ms     = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.UTF8);
        using var csv    = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        // Headers
        if (includeRespondentInfo)
        {
            csv.WriteField("ResponseId");
            csv.WriteField("RespondentName");
            csv.WriteField("RespondentEmail");
        }
        else
        {
            csv.WriteField("ResponseId");
        }
        csv.WriteField("SubmittedAt");

        foreach (var q in survey.Questions)
            csv.WriteField(SanitiseCsvValue(q.QuestionText));

        await csv.NextRecordAsync();

        // Rows
        foreach (var response in responses)
        {
            if (includeRespondentInfo)
            {
                csv.WriteField(response.Id.ToString());
                csv.WriteField(SanitiseCsvValue(response.User.FullName));
                csv.WriteField(SanitiseCsvValue(response.User.Email));
            }
            else
            {
                csv.WriteField(response.Id.ToString());
            }
            csv.WriteField(response.SubmittedAt.ToString("o"));

            foreach (var q in survey.Questions)
            {
                var answer = response.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
                var value  = answer is null ? string.Empty
                           : q.QuestionType == SurveyQuestionType.Rating ? answer.RatingValue?.ToString() ?? string.Empty
                           : q.QuestionType == SurveyQuestionType.MultipleChoice ? answer.AnswerOptionsJson ?? string.Empty
                           : answer.AnswerText ?? string.Empty;
                csv.WriteField(SanitiseCsvValue(value));
            }
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync(ct);
        var fileName = $"survey-{request.SurveyId}-responses-{DateTime.UtcNow:yyyyMMdd}.csv";
        return (ms.ToArray(), fileName);
    }

    public async Task<(byte[] Data, string FileName)> ExportToPdfAsync(SurveyExportRequest request, CancellationToken ct = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var summary  = await GetDashboardAsync(request.SurveyId, ct);
        var funnel   = await GetParticipationFunnelAsync(request.SurveyId, ct);
        var qStats   = await GetQuestionStatsAsync(request.SurveyId, null, null, request.FromDate, request.ToDate, ct);

        var survey = await _db.Surveys
            .Where(s => s.Id == request.SurveyId && s.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (survey is null) throw new NotFoundException("Survey", request.SurveyId);

        SurveyNpsReportDto? nps = null;
        try { nps = await GetNpsReportAsync(request.SurveyId, ct); } catch { }

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Content().Column(col =>
                {
                    col.Item().Text(survey.Title).FontSize(20).Bold();
                    col.Item().Text($"Generated: {DateTime.UtcNow:MMMM dd, yyyy}").FontSize(10);
                    col.Item().PaddingTop(10).Text("Executive Summary").FontSize(14).Bold();
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                        t.Cell().Text("Total Invited");       t.Cell().Text(summary.TotalInvited.ToString());
                        t.Cell().Text("Total Submitted");     t.Cell().Text(summary.TotalSubmitted.ToString());
                        t.Cell().Text("Response Rate");       t.Cell().Text($"{summary.ResponseRatePct:F1}%");
                        t.Cell().Text("Health Status");       t.Cell().Text(summary.HealthStatus);
                    });

                    col.Item().PaddingTop(10).Text("Participation Funnel").FontSize(14).Bold();
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                        t.Cell().Text("Stage");  t.Cell().Text("Count"); t.Cell().Text("Rate");
                        t.Cell().Text("Invited");    t.Cell().Text(funnel.TotalInvited.ToString());   t.Cell().Text("—");
                        t.Cell().Text("Emails Sent"); t.Cell().Text(funnel.TotalEmailsSent.ToString()); t.Cell().Text("—");
                        t.Cell().Text("Link Opened"); t.Cell().Text(funnel.TotalTokensAccessed.ToString()); t.Cell().Text("—");
                        t.Cell().Text("Submitted");  t.Cell().Text(funnel.TotalSubmitted.ToString());  t.Cell().Text($"{funnel.SubmissionRatePct:F1}%");
                    });

                    if (nps != null)
                    {
                        col.Item().PaddingTop(10).Text("NPS Score").FontSize(14).Bold();
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                            t.Cell().Text("NPS Score"); t.Cell().Text("Promoters"); t.Cell().Text("Passives"); t.Cell().Text("Detractors");
                            t.Cell().Text(nps.NpsScore.ToString());
                            t.Cell().Text($"{nps.Promoters} ({nps.PromoterPct:F1}%)");
                            t.Cell().Text($"{nps.Passives} ({nps.PassivePct:F1}%)");
                            t.Cell().Text($"{nps.Detractors} ({nps.DetractorPct:F1}%)");
                        });
                    }

                    col.Item().PaddingTop(10).Text("Question Statistics").FontSize(14).Bold();
                    foreach (var qs in qStats)
                    {
                        col.Item().PaddingTop(5).Text(qs.QuestionText).Bold();
                        col.Item().Text($"Answers: {qs.TotalAnswers} | Type: {qs.QuestionType}");
                        if (qs.AverageRating.HasValue)
                            col.Item().Text($"Avg: {qs.AverageRating:F2} / Min: {qs.MinRating} / Max: {qs.MaxRating}");
                        foreach (var opt in qs.OptionStats.Take(10))
                            col.Item().Text($"  • {opt.OptionValue}: {opt.Count} ({opt.Percentage:F1}%)");
                    }
                });
            });
        });

        using var ms = new MemoryStream();
        doc.GeneratePdf(ms);
        var fileName = $"survey-{request.SurveyId}-report-{DateTime.UtcNow:yyyyMMdd}.pdf";
        return (ms.ToArray(), fileName);
    }

    // -- Private helpers ----------------------------------------------------

    private async Task<SurveyQuestionAnalyticsDto?> GetQuestionStatsForTextAsync(
        Guid surveyId, string questionTextLower, CancellationToken ct)
    {
        var question = await _db.SurveyQuestions
            .Where(q => q.SurveyId == surveyId
                     && q.QuestionText.ToLower().Trim() == questionTextLower)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (question is null) return null;

        var stats = await GetQuestionStatsAsync(surveyId, null, null, null, null, ct);
        return stats.FirstOrDefault(s => s.QuestionId == question.Id);
    }

    /// <summary>Sanitises a CSV cell value against injection attacks (OWASP A03).</summary>
    private static string SanitiseCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        // Prefix formula-injection characters with a tab to neutralise them
        if (value[0] is '=' or '+' or '-' or '@')
            value = "\t" + value;
        return value;
    }
}
