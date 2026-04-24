using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowHub.Infrastructure.BackgroundServices;

/// <summary>
/// Computes trending scores for community posts and writes them to the Redis sorted set.
/// Runs every 5 minutes.
/// Formula: reactions × 3 + comments × 2 + bookmarks × 1, dampened by post age.
/// </summary>
public class TrendingScorerService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TrendingScorerService> _logger;

    public TrendingScorerService(IServiceProvider services, ILogger<TrendingScorerService> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TrendingScorerService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ComputeTrendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in TrendingScorerService tick.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task ComputeTrendingAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db    = scope.ServiceProvider.GetRequiredService<KnowHubDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCommunityCache>();

        var since = DateTime.UtcNow.AddDays(-7);

        var tenantPosts = await db.CommunityPosts
            .Where(p => p.Status == PostStatus.Published && p.PublishedAt >= since)
            .AsNoTracking()
            .Select(p => new
            {
                p.TenantId,
                p.Id,
                p.ReactionCount,
                p.CommentCount,
                p.BookmarkCount,
                p.PublishedAt
            })
            .ToListAsync(ct);

        var grouped = tenantPosts.GroupBy(p => p.TenantId);

        foreach (var tenantGroup in grouped)
        {
            foreach (var post in tenantGroup)
            {
                var ageHours = (DateTime.UtcNow - (post.PublishedAt ?? DateTime.UtcNow)).TotalHours;
                var decay    = 1.0 / Math.Max(1.0, Math.Log(ageHours + 2, 2));
                var score    = (post.ReactionCount * 3 + post.CommentCount * 2 + post.BookmarkCount) * decay;

                await cache.AddToTrendingAsync(tenantGroup.Key, post.Id, score, ct);
            }

            _logger.LogDebug("TrendingScorerService: scored {Count} posts for tenant {TenantId}.",
                tenantGroup.Count(), tenantGroup.Key);
        }
    }
}
