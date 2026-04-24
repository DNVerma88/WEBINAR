using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.PeerReview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/knowledge-assets")]
[Authorize]
public class KnowledgeAssetsController : ControllerBase
{
    private readonly IKnowledgeAssetService _knowledgeAssetService;
    private readonly IPeerReviewService _peerReviewService;

    public KnowledgeAssetsController(
        IKnowledgeAssetService knowledgeAssetService,
        IPeerReviewService peerReviewService)
    {
        _knowledgeAssetService = knowledgeAssetService;
        _peerReviewService = peerReviewService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssets([FromQuery] GetAssetsRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledgeAssetService.GetAssetsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(KnowledgeAssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsset(Guid id, CancellationToken cancellationToken)
    {
        var result = await _knowledgeAssetService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(KnowledgeAssetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAsset([FromBody] CreateKnowledgeAssetRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledgeAssetService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAsset), new { id = result.Id }, result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsset(Guid id, CancellationToken cancellationToken)
    {
        await _knowledgeAssetService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // Peer Review endpoints
    [HttpPost("{id:guid}/reviews/nominate")]
    [ProducesResponseType(typeof(AssetReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> NominateReviewer(
        Guid id, [FromBody] NominateReviewerRequest request, CancellationToken cancellationToken)
    {
        var requestWithId = request with { KnowledgeAssetId = id };
        var result = await _peerReviewService.NominateReviewerAsync(requestWithId, cancellationToken);
        return CreatedAtAction(nameof(GetAssetReviews), new { id }, result);
    }

    [HttpGet("{id:guid}/reviews")]
    [ProducesResponseType(typeof(List<AssetReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssetReviews(Guid id, CancellationToken cancellationToken)
    {
        var result = await _peerReviewService.GetAssetReviewsAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/reviews/{reviewId:guid}")]
    [ProducesResponseType(typeof(AssetReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitReview(
        Guid id, Guid reviewId, [FromBody] SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        var result = await _peerReviewService.SubmitReviewAsync(reviewId, request, cancellationToken);
        return Ok(result);
    }

    // B15: pending review queue is for KnowledgeTeam / Admin only
    [HttpGet("reviews/pending")]
    [Authorize(Policy = "KnowledgeTeamOrAbove")]
    [ProducesResponseType(typeof(List<AssetReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingReviews(
        [FromQuery] GetPendingReviewsRequest request, CancellationToken cancellationToken)
    {
        var result = await _peerReviewService.GetPendingReviewsAsync(request, cancellationToken);
        return Ok(result);
    }
}
