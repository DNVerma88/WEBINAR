using KnowHub.Application.Contracts.SpeakerMarketplace;
using KnowHub.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/speaker-marketplace")]
[Authorize]
public class SpeakerMarketplaceController : ControllerBase
{
    private readonly ISpeakerMarketplaceService _marketplaceService;

    public SpeakerMarketplaceController(ISpeakerMarketplaceService marketplaceService)
    {
        _marketplaceService = marketplaceService;
    }

    [HttpGet("available")]
    [ProducesResponseType(typeof(PagedResult<SpeakerAvailabilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableSpeakers(
        [FromQuery] GetAvailableSpeakersRequest request, CancellationToken cancellationToken)
    {
        var result = await _marketplaceService.GetAvailableSpeakersAsync(request, cancellationToken);
        return Ok(result);
    }

    // B16: only Contributors (and above) can advertise speaker availability
    [HttpPost("availability")]
    [Authorize(Policy = "ContributorOrAbove")]
    [ProducesResponseType(typeof(SpeakerAvailabilityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetAvailability(
        [FromBody] SetAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var result = await _marketplaceService.SetAvailabilityAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAvailableSpeakers), null, result);
    }

    [HttpPut("availability/{id:guid}")]
    [ProducesResponseType(typeof(SpeakerAvailabilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAvailability(
        Guid id, [FromBody] UpdateAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var result = await _marketplaceService.UpdateAvailabilityAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("availability/my")]
    [ProducesResponseType(typeof(List<SpeakerAvailabilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAvailability(CancellationToken cancellationToken)
    {
        var result = await _marketplaceService.GetMyAvailabilityAsync(cancellationToken);
        return Ok(result);
    }

    [HttpDelete("availability/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAvailability(Guid id, CancellationToken cancellationToken)
    {
        await _marketplaceService.DeleteAvailabilityAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("bookings")]
    [ProducesResponseType(typeof(SpeakerBookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestBooking(
        [FromBody] RequestBookingRequest request, CancellationToken cancellationToken)
    {
        var result = await _marketplaceService.RequestBookingAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetMyBookings), null, result);
    }

    [HttpGet("bookings")]
    [ProducesResponseType(typeof(PagedResult<SpeakerBookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBookings(
        [FromQuery] GetMyBookingsRequest request, CancellationToken cancellationToken)
    {
        var result = await _marketplaceService.GetMyBookingsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("bookings/{id:guid}/respond")]
    [ProducesResponseType(typeof(SpeakerBookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RespondToBooking(
        Guid id, [FromBody] RespondToBookingRequest request, CancellationToken cancellationToken)
    {
        var result = await _marketplaceService.RespondToBookingAsync(id, request, cancellationToken);
        return Ok(result);
    }
    [HttpPut("bookings/{id:guid}/link-session")]
    [ProducesResponseType(typeof(SpeakerBookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkToSession(
        Guid id, [FromBody] LinkBookingToSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _marketplaceService.LinkToSessionAsync(id, request.SessionId, cancellationToken);
        return Ok(result);
    }
    [HttpPut("bookings/{id:guid}/complete")]
    [ProducesResponseType(typeof(SpeakerBookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteBooking(Guid id, CancellationToken cancellationToken)
    {
        var result = await _marketplaceService.CompleteBookingAsync(id, cancellationToken);
        return Ok(result);
    }

    // Admin/KT only: directly assign a speaker to a session without the request/accept cycle
    [HttpPost("admin-assign")]
    [Authorize(Policy = "KnowledgeTeamOrAbove")]
    [ProducesResponseType(typeof(SpeakerBookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminAssign(
        [FromBody] AdminAssignRequest request, CancellationToken cancellationToken)
    {
        var result = await _marketplaceService.AdminAssignAsync(request, cancellationToken);
        return Ok(result);
    }
}
