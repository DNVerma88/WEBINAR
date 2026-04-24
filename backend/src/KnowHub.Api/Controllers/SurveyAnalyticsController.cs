using KnowHub.Application.Contracts.Surveys;
using KnowHub.Application.Models.Surveys.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/surveys")]
[Authorize(Policy = "AdminOrAbove")]
public class SurveyAnalyticsController : ControllerBase
{
    private readonly ISurveyAnalyticsService _analytics;

    public SurveyAnalyticsController(ISurveyAnalyticsService analytics)
        => _analytics = analytics;

    [HttpGet("{id:guid}/analytics/dashboard")]
    public async Task<IActionResult> GetDashboard(Guid id, CancellationToken ct)
        => Ok(await _analytics.GetDashboardAsync(id, ct));

    [HttpGet("{id:guid}/analytics/questions")]
    public async Task<IActionResult> GetQuestionStats(
        Guid id,
        [FromQuery] string?   department,
        [FromQuery] string?   role,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
        => Ok(await _analytics.GetQuestionStatsAsync(id, department, role, fromDate, toDate, ct));

    [HttpGet("{id:guid}/analytics/questions/{questionId:guid}/department-breakdown")]
    public async Task<IActionResult> GetDepartmentBreakdown(Guid id, Guid questionId, CancellationToken ct)
        => Ok(await _analytics.GetDepartmentBreakdownAsync(id, questionId, ct));

    [HttpGet("{id:guid}/analytics/nps")]
    public async Task<IActionResult> GetNpsReport(Guid id, CancellationToken ct)
        => Ok(await _analytics.GetNpsReportAsync(id, ct));

    /// <summary>?surveyIds=guid1,guid2,guid3 (comma-separated, max 10)</summary>
    [HttpGet("analytics/nps-trend")]
    public async Task<IActionResult> GetNpsTrend([FromQuery] string surveyIds, CancellationToken ct)
    {
        var ids = (surveyIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        if (ids.Count == 0)
            return BadRequest(new { error = "Provide at least one valid survey ID." });
        if (ids.Count > 10)
            return BadRequest(new { error = "At most 10 survey IDs are allowed per request." });

        return Ok(await _analytics.GetNpsTrendAsync(ids, ct));
    }

    [HttpGet("{id:guid}/analytics/funnel")]
    public async Task<IActionResult> GetParticipationFunnel(Guid id, CancellationToken ct)
        => Ok(await _analytics.GetParticipationFunnelAsync(id, ct));

    [HttpGet("{id:guid}/analytics/heatmap")]
    public async Task<IActionResult> GetHeatmap(Guid id, CancellationToken ct)
        => Ok(await _analytics.GetHeatmapAsync(id, ct));

    [HttpGet("analytics/compare")]
    public async Task<IActionResult> CompareSurveys(
        [FromQuery] Guid a, [FromQuery] Guid b, CancellationToken ct)
        => Ok(await _analytics.CompareSurveysAsync(a, b, ct));

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(
        Guid         id,
        [FromQuery] ExportFormat format,
        [FromQuery] bool         includeRespondentInfo = false,
        [FromQuery] DateTime?    fromDate = null,
        [FromQuery] DateTime?    toDate   = null,
        CancellationToken        ct       = default)
    {
        var request = new SurveyExportRequest(id, format, includeRespondentInfo, fromDate, toDate);

        if (format == ExportFormat.Csv)
        {
            var (data, fileName) = await _analytics.ExportToCsvAsync(request, ct);
            return File(data, "text/csv", fileName);
        }
        else
        {
            var (data, fileName) = await _analytics.ExportToPdfAsync(request, ct);
            return File(data, "application/pdf", fileName);
        }
    }
}
