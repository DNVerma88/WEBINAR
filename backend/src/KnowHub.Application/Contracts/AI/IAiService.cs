using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts.AI;

public interface IAiService
{
    Task<AiSummaryResponse> GenerateSessionSummaryAsync(Guid sessionId, string transcriptText, CancellationToken cancellationToken);
    Task<List<LearningPathRecommendationDto>> GetPersonalisedRecommendationsAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<KnowledgeGapDto>> DetectKnowledgeGapsAsync(Guid userId, CancellationToken cancellationToken);
    Task<PagedResult<TranscriptSearchResultDto>> SearchTranscriptAsync(TranscriptSearchRequest request, CancellationToken cancellationToken);
}
