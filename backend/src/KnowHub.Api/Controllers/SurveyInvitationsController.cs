using KnowHub.Application.Contracts.Surveys;
using KnowHub.Application.Models.Surveys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KnowHub.Api.Controllers;

/// <summary>
/// Survey invitation management: list invitations and resend tokens.
/// Resend endpoints are rate-limited to prevent abuse.
/// </summary>
[ApiController]
[Route("api/surveys/{surveyId:guid}/invitations")]
[Authorize(Policy = "AdminOrAbove")]
public class SurveyInvitationsController : ControllerBase
{
    private readonly ISurveyInvitationService _invitationService;

    public SurveyInvitationsController(ISurveyInvitationService invitationService)
        => _invitationService = invitationService;

    /// <summary>
    /// Paginated list of invitations for a survey. Filterable by status.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetInvitations(
        Guid surveyId,
        [FromQuery] GetInvitationsRequest request,
        CancellationToken ct)
        => Ok(await _invitationService.GetInvitationsAsync(surveyId, request, ct));

    /// <summary>
    /// Resend invitation to a single employee. Generates a new token; previous token is expired.
    /// Blocked if the invitation is already Submitted.
    /// </summary>
    [HttpPost("{userId:guid}/resend")]
    [EnableRateLimiting("AdminResendPolicy")]
    public async Task<IActionResult> ResendToUser(
        Guid surveyId,
        Guid userId,
        CancellationToken ct)
    {
        await _invitationService.ResendToUserAsync(surveyId, userId, ct);
        return NoContent();
    }

    /// <summary>
    /// Resend invitations to a specific list of employees (max 500 per request).
    /// Skips any userId whose invitation is already Submitted.
    /// </summary>
    [HttpPost("resend-bulk")]
    [EnableRateLimiting("AdminResendPolicy")]
    public async Task<IActionResult> ResendBulk(
        Guid surveyId,
        [FromBody] ResendInvitationsRequest request,
        CancellationToken ct)
    {
        await _invitationService.ResendBulkAsync(surveyId, request, ct);
        return NoContent();
    }

    /// <summary>
    /// Resend to all employees whose token has expired and who have not yet submitted.
    /// Returns 202 Accepted — resend is processed asynchronously.
    /// </summary>
    [HttpPost("resend-all-pending")]
    [EnableRateLimiting("AdminResendPolicy")]
    public async Task<IActionResult> ResendAllPending(
        Guid surveyId,
        CancellationToken ct)
    {
        await _invitationService.ResendAllPendingAsync(surveyId, ct);
        return Accepted(new { message = "Resend queued for all expired invitations." });
    }
}
