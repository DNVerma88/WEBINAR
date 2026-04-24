using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/session-proposals")]
[Authorize]
public class SessionProposalsController : ControllerBase
{
    private readonly ISessionProposalService _proposalService;

    public SessionProposalsController(ISessionProposalService proposalService) => _proposalService = proposalService;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProposals([FromQuery] GetSessionProposalsRequest request, CancellationToken cancellationToken)
    {
        var result = await _proposalService.GetProposalsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SessionProposalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProposal(Guid id, CancellationToken cancellationToken)
    {
        var result = await _proposalService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SessionProposalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProposal([FromBody] CreateSessionProposalRequest request, CancellationToken cancellationToken)
    {
        var result = await _proposalService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProposal), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SessionProposalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProposal(Guid id, [FromBody] UpdateSessionProposalRequest request, CancellationToken cancellationToken)
    {
        var result = await _proposalService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProposal(Guid id, CancellationToken cancellationToken)
    {
        await _proposalService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(typeof(SessionProposalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SubmitProposal(Guid id, CancellationToken cancellationToken)
    {
        var result = await _proposalService.SubmitAsync(id, cancellationToken);
        return Ok(result);
    }

    // B12: approval workflow — require Manager or above at controller level
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "ManagerOrAbove")]
    [ProducesResponseType(typeof(SessionProposalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveProposal(Guid id, [FromBody] ApproveProposalRequest? request, CancellationToken cancellationToken)
    {
        var result = await _proposalService.ApproveAsync(id, request ?? new ApproveProposalRequest(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "ManagerOrAbove")]
    [ProducesResponseType(typeof(SessionProposalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectProposal(Guid id, [FromBody] RejectProposalRequest request, CancellationToken cancellationToken)
    {
        var result = await _proposalService.RejectAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/request-revision")]
    [Authorize(Policy = "ManagerOrAbove")]
    [ProducesResponseType(typeof(SessionProposalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestRevision(Guid id, [FromBody] RequestRevisionRequest request, CancellationToken cancellationToken)
    {
        var result = await _proposalService.RequestRevisionAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
