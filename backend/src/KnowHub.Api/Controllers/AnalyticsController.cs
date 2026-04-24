using KnowHub.Application.Contracts.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Policy = "ManagerOrAbove")]  // B3: analytics require Manager / KnowledgeTeam / Admin
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(AnalyticsSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("knowledge-gap-heatmap")]
    [ProducesResponseType(typeof(KnowledgeGapHeatmapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetKnowledgeGapHeatmap(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetKnowledgeGapHeatmapAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("skill-coverage")]
    [ProducesResponseType(typeof(SkillCoverageReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSkillCoverage(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetSkillCoverageAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("content-freshness")]
    [ProducesResponseType(typeof(ContentFreshnessReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetContentFreshness(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetContentFreshnessAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("learning-funnel")]
    [ProducesResponseType(typeof(LearningFunnelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLearningFunnel(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetLearningFunnelAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("cohort-completion")]
    [ProducesResponseType(typeof(CohortCompletionRatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCohortCompletion(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetCohortCompletionRatesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("department-engagement")]
    [ProducesResponseType(typeof(DepartmentEngagementScoreResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDepartmentEngagement(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetDepartmentEngagementAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("knowledge-retention")]
    [ProducesResponseType(typeof(KnowledgeRetentionScoreResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetKnowledgeRetention(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetKnowledgeRetentionAsync(cancellationToken);
        return Ok(result);
    }
}
