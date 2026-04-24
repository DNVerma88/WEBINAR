using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

/// <summary>Admin/Moderator endpoint for managing community content reports.</summary>
[ApiController]
[Route("api/moderation/community")]
[Authorize(Policy = "AdminOrAbove")]
public class CommunityModerationController : ControllerBase
{
    private readonly IContentModerationService _moderationService;

    public CommunityModerationController(IContentModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    [HttpGet("reports")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpenReports([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _moderationService.GetOpenReportsAsync(pageNumber, pageSize, ct);
        return Ok(result);
    }

    [HttpPost("reports/{reportId:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveReport(Guid reportId, [FromBody] ResolveReportRequest request, CancellationToken ct)
    {
        await _moderationService.ResolveReportAsync(reportId, request, ct);
        return NoContent();
    }

    [HttpPost("reports/{reportId:guid}/dismiss")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissReport(Guid reportId, CancellationToken ct)
    {
        await _moderationService.DismissReportAsync(reportId, ct);
        return NoContent();
    }
}
