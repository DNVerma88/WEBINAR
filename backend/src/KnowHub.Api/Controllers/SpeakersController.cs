using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/speakers")]
[Authorize]
public class SpeakersController : ControllerBase
{
    private readonly ISpeakerService _speakerService;

    public SpeakersController(ISpeakerService speakerService) => _speakerService = speakerService;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpeakers([FromQuery] GetSpeakersRequest request, CancellationToken cancellationToken)
    {
        var result = await _speakerService.GetSpeakersAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SpeakerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpeaker(Guid id, CancellationToken cancellationToken)
    {
        var result = await _speakerService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }
}
