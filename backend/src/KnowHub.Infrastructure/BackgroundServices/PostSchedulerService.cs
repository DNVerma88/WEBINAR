using KnowHub.Domain.Enums;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowHub.Infrastructure.BackgroundServices;

/// <summary>
/// Publishes scheduled community posts when their ScheduledAt time has passed.
/// Runs every 60 seconds.
/// </summary>
public class PostSchedulerService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PostSchedulerService> _logger;

    public PostSchedulerService(IServiceProvider services, ILogger<PostSchedulerService> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PostSchedulerService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishDuePostsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in PostSchedulerService tick.");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    private async Task PublishDuePostsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowHubDbContext>();

        var now  = DateTime.UtcNow;
        var rows = await db.CommunityPosts
            .Where(p => p.Status == PostStatus.Scheduled
                        && p.ScheduledAt.HasValue
                        && p.ScheduledAt <= now)
            .ToListAsync(ct);

        if (rows.Count == 0) return;

        _logger.LogInformation("PostSchedulerService: publishing {Count} scheduled post(s).", rows.Count);

        foreach (var post in rows)
        {
            post.Status      = PostStatus.Published;
            post.PublishedAt = post.ScheduledAt ?? now;
            post.ModifiedOn  = now;
        }

        await db.SaveChangesAsync(ct);
    }
}
