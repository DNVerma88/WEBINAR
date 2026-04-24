using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/knowledge-requests")]
[Authorize]
public class KnowledgeRequestsController : ControllerBase
{
    private readonly IKnowledgeRequestService _knowledgeRequestService;

    public KnowledgeRequestsController(IKnowledgeRequestService knowledgeRequestService)
        => _knowledgeRequestService = knowledgeRequestService;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequests([FromQuery] GetKnowledgeRequestsRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledgeRequestService.GetRequestsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(KnowledgeRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRequest([FromBody] CreateKnowledgeRequestRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledgeRequestService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRequests), new { }, result);
    }

    [HttpPost("{id:guid}/upvote")]
    [ProducesResponseType(typeof(KnowledgeRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Upvote(Guid id, CancellationToken cancellationToken)
    {
        var result = await _knowledgeRequestService.UpvoteAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/claim")]
    [ProducesResponseType(typeof(KnowledgeRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Claim(Guid id, CancellationToken cancellationToken)
    {
        var result = await _knowledgeRequestService.ClaimAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(KnowledgeRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _knowledgeRequestService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(KnowledgeRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseKnowledgeRequestRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledgeRequestService.CloseAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/address")]
    [ProducesResponseType(typeof(KnowledgeRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Address(Guid id, [FromBody] AddressKnowledgeRequestRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledgeRequestService.AddressAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
