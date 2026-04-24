using KnowHub.Application.Contracts.Moderation;
using KnowHub.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/moderation")]
[Authorize(Policy = "AdminOnly")]  // B2: moderation actions require Admin or SuperAdmin
public class ModerationController : ControllerBase
{
    private readonly IModerationService _moderationService;

    public ModerationController(IModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    [HttpPost("flags")]
    [ProducesResponseType(typeof(ContentFlagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FlagContent(
        [FromBody] FlagContentRequest request, CancellationToken cancellationToken)
    {
        var result = await _moderationService.FlagContentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetContentFlags), null, result);
    }

    [HttpGet("flags")]
    [ProducesResponseType(typeof(PagedResult<ContentFlagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetContentFlags(
        [FromQuery] GetContentFlagsRequest request, CancellationToken cancellationToken)
    {
        var result = await _moderationService.GetContentFlagsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("flags/{id:guid}/review")]
    [ProducesResponseType(typeof(ContentFlagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewFlag(
        Guid id, [FromBody] ReviewFlagRequest request, CancellationToken cancellationToken)
    {
        var result = await _moderationService.ReviewFlagAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("users/{id:guid}/suspend")]
    [ProducesResponseType(typeof(UserSuspensionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendUser(
        Guid id, [FromBody] SuspendUserRequest request, CancellationToken cancellationToken)
    {
        var requestWithId = request with { UserId = id };
        var result = await _moderationService.SuspendUserAsync(requestWithId, cancellationToken);
        return CreatedAtAction(nameof(GetUserSuspensions), new { id }, result);
    }

    [HttpPut("suspensions/{id:guid}/lift")]
    [ProducesResponseType(typeof(UserSuspensionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LiftSuspension(
        Guid id, [FromBody] LiftSuspensionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _moderationService.LiftSuspensionAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("suspensions")]
    [ProducesResponseType(typeof(PagedResult<UserSuspensionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActiveSuspensions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _moderationService.GetActiveSuspensionsAsync(pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("users/{id:guid}/suspensions")]
    [ProducesResponseType(typeof(List<UserSuspensionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserSuspensions(Guid id, CancellationToken cancellationToken)
    {
        var result = await _moderationService.GetUserSuspensionsAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("sessions/bulk-status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BulkUpdateSessionStatus(
        [FromBody] BulkSessionStatusRequest request, CancellationToken cancellationToken)
    {
        await _moderationService.BulkUpdateSessionStatusAsync(request, cancellationToken);
        return NoContent();
    }
}
