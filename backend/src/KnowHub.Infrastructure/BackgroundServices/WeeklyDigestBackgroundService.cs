using KnowHub.Application.Contracts.Email;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowHub.Infrastructure.BackgroundServices;

public class WeeklyDigestBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeeklyDigestBackgroundService> _logger;

    public WeeklyDigestBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<WeeklyDigestBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextMonday();
            _logger.LogInformation("Weekly digest scheduler: next run in {Delay}", delay);
            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
                await SendDigestsAsync(stoppingToken);
        }
    }

    private async Task SendDigestsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting weekly digest email generation");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowHubDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var weekStart = DateTime.UtcNow.AddDays(-7);

        var users = await db.Users
            .Where(u => u.IsActive)
            .AsNoTracking()
            .Select(u => new { u.Id, u.TenantId, u.FullName, u.Email })
            .ToListAsync(cancellationToken);

        var topSessions = await db.Sessions
            .Where(s => s.CreatedDate >= weekStart)
            .OrderByDescending(s => s.Registrations.Count)
            .Take(5)
            .AsNoTracking()
            .Select(s => new
            {
                s.Id,
                s.Title,
                s.ScheduledAt,
                RegisteredCount = s.Registrations.Count,
                SpeakerName = s.Speaker != null ? s.Speaker.FullName : "Unknown"
            })
            .ToListAsync(cancellationToken);

        var digestSessions = topSessions.Select(s => new DigestSessionItem(
            s.Id, s.Title, s.SpeakerName, s.ScheduledAt, s.RegisteredCount)).ToList();

        foreach (var user in users)
        {
            try
            {
                var xpThisWeek = await db.UserXpEvents
                    .Where(e => e.UserId == user.Id && e.TenantId == user.TenantId && e.CreatedDate >= weekStart)
                    .SumAsync(e => e.XpAmount, cancellationToken);

                var leaderboard = await db.LeaderboardSnapshots
                    .Where(s => s.TenantId == user.TenantId)
                    .OrderByDescending(s => s.SnapshotYear)
                    .ThenByDescending(s => s.SnapshotMonth)
                    .FirstOrDefaultAsync(cancellationToken);

                var rank = 0;
                if (leaderboard is not null)
                {
                    rank = GetUserRankFromSnapshot(leaderboard.Entries, user.Id);
                }

                var unreadCount = await db.Notifications
                    .CountAsync(n => n.UserId == user.Id && !n.IsRead, cancellationToken);

                var data = new WeeklyDigestEmailData(
                    user.FullName,
                    user.Email,
                    user.Id,
                    user.TenantId,
                    xpThisWeek,
                    rank,
                    unreadCount,
                    digestSessions,
                    new List<DigestCommunityItem>()
                );

                await emailService.SendWeeklyDigestAsync(data, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send weekly digest to user {UserId}", user.Id);
            }
        }

        _logger.LogInformation("Weekly digest completed for {Count} users", users.Count);
    }

    private static int GetUserRankFromSnapshot(string entries, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(entries)) return 0;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(entries);
            var root = doc.RootElement;
            if (root.ValueKind != System.Text.Json.JsonValueKind.Array)
                return 0;

            foreach (var entry in root.EnumerateArray())
            {
                if (entry.TryGetProperty("userId", out var uid) &&
                    uid.TryGetGuid(out var entryUserId) &&
                    entryUserId == userId &&
                    entry.TryGetProperty("rank", out var rankProp))
                {
                    return rankProp.GetInt32();
                }
            }
        }
        catch
        {
            return 0;
        }

        return 0;
    }

    private static TimeSpan ComputeDelayUntilNextMonday()
    {
        var now = DateTime.UtcNow;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0 && now.Hour >= 8)
            daysUntilMonday = 7;

        var nextMonday = now.Date.AddDays(daysUntilMonday).AddHours(8);
        return nextMonday - now;
    }
}
