using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/mentoring")]
[Authorize]
public class MentoringController : ControllerBase
{
    private readonly IMentoringService _mentoringService;

    public MentoringController(IMentoringService mentoringService)
        => _mentoringService = mentoringService;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    // B22: expose pagination so large tenants can page through all pairings
    public async Task<IActionResult> GetPairings(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mentoringService.GetPairingsAsync(pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("requests")]
    [ProducesResponseType(typeof(MentorMenteeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestMentor([FromBody] RequestMentorRequest request, CancellationToken cancellationToken)
    {
        var result = await _mentoringService.RequestMentorAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPairings), result);
    }

    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(typeof(MentorMenteeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mentoringService.AcceptAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/decline")]
    [ProducesResponseType(typeof(MentorMenteeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Decline(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mentoringService.DeclineAsync(id, cancellationToken);
        return Ok(result);
    }
}
