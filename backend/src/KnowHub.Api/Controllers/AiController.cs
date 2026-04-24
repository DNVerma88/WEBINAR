using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.AI;
using KnowHub.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
[EnableRateLimiting("AiPolicy")]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly ICurrentUserAccessor _currentUser;

    public AiController(IAiService aiService, ICurrentUserAccessor currentUser)
    {
        _aiService = aiService;
        _currentUser = currentUser;
    }

    // B1 fix: userId is no longer accepted from the query string — always uses the authenticated user's ID
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(List<LearningPathRecommendationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecommendations(CancellationToken cancellationToken)
    {
        var result = await _aiService.GetPersonalisedRecommendationsAsync(_currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("knowledge-gaps")]
    [ProducesResponseType(typeof(List<KnowledgeGapDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKnowledgeGaps(CancellationToken cancellationToken)
    {
        var result = await _aiService.DetectKnowledgeGapsAsync(_currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("search/transcripts")]
    [ProducesResponseType(typeof(PagedResult<TranscriptSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTranscripts(
        [FromQuery] TranscriptSearchRequest request, CancellationToken cancellationToken)
    {
        var result = await _aiService.SearchTranscriptAsync(request, cancellationToken);
        return Ok(result);
    }

    // B21 fix: restrict expensive LLM call to KnowledgeTeam / Admin / SuperAdmin
    [HttpPost("sessions/{id:guid}/summary")]
    [Authorize(Policy = "KnowledgeTeamOrAbove")]
    [ProducesResponseType(typeof(AiSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateSessionSummary(
        Guid id, [FromBody] AiSummaryRequest request, CancellationToken cancellationToken)
    {
        var result = await _aiService.GenerateSessionSummaryAsync(id, request.TranscriptText, cancellationToken);
        return Ok(result);
    }
}
