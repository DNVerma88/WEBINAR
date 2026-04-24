using KnowHub.Application.Contracts;

namespace KnowHub.Infrastructure.Services;

/// <summary>No-op implementation used when Redis is not configured (dev/test without Redis).</summary>
public class NoOpCommunityCache : IDistributedCommunityCache
{
    public Task IncrementViewAsync(Guid postId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<Dictionary<Guid, long>> FlushViewBufferAsync(CancellationToken ct = default) => Task.FromResult(new Dictionary<Guid, long>());
    public Task InvalidateFeedAsync(Guid tenantId, Guid userId, CancellationToken ct = default) => Task.CompletedTask;
    public Task AddToTrendingAsync(Guid tenantId, Guid postId, double score, CancellationToken ct = default) => Task.CompletedTask;
    public Task<IReadOnlyList<Guid>> GetTrendingPostIdsAsync(Guid tenantId, int count, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
}
