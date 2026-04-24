namespace KnowHub.Application.Contracts;

public interface IDistributedCommunityCache
{
    Task IncrementViewAsync(Guid postId, CancellationToken ct = default);
    Task<Dictionary<Guid, long>> FlushViewBufferAsync(CancellationToken ct = default);
    Task InvalidateFeedAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task AddToTrendingAsync(Guid tenantId, Guid postId, double score, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetTrendingPostIdsAsync(Guid tenantId, int count, CancellationToken ct = default);
}
