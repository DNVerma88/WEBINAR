using KnowHub.Application.Contracts.Email;
using KnowHub.Domain.Enums;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowHub.Infrastructure.BackgroundServices;

/// <summary>
/// Sends a daily community digest email at 09:00 UTC to active users who are members
/// of at least one community, highlighting the top 5 posts published in the last 24 hours.
/// </summary>
public class CommunityDigestService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CommunityDigestService> _logger;

    public CommunityDigestService(IServiceProvider services, ILogger<CommunityDigestService> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CommunityDigestService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNext9Am();
            _logger.LogInformation("CommunityDigestService: next digest in {Delay}.", delay);
            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
                await SendDigestsAsync(stoppingToken);
        }
    }

    private async Task SendDigestsAsync(CancellationToken ct)
    {
        _logger.LogInformation("CommunityDigestService: sending daily digests.");

        using var scope = _services.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<KnowHubDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var since = DateTime.UtcNow.AddHours(-24);

        // Get top posts published in the last 24h, across all tenants
        var topPosts = await db.CommunityPosts
            .Where(p => p.Status == PostStatus.Published
                        && p.PublishedAt >= since)
            .OrderByDescending(p => p.ReactionCount * 3 + p.CommentCount * 2)
            .Take(5)
            .Include(p => p.Author)
            .Include(p => p.Community)
            .AsNoTracking()
            .Select(p => new
            {
                p.Title,
                p.Slug,
                p.CommunityId,
                CommunitySlug  = p.Community!.Slug,
                CommunityName  = p.Community.Name,
                AuthorName     = p.Author!.FullName,
                p.ReactionCount,
                p.TenantId
            })
            .ToListAsync(ct);

        if (topPosts.Count == 0)
        {
            _logger.LogInformation("CommunityDigestService: no posts published today, skipping digest.");
            return;
        }

        // Build per-tenant digest emails
        var tenantGroups = topPosts.GroupBy(p => p.TenantId);

        foreach (var tenant in tenantGroups)
        {
            var members = await db.CommunityMembers
                .Where(m => m.TenantId == tenant.Key)
                .Include(m => m.User)
                .AsNoTracking()
                .Select(m => new { m.User!.Email, m.User.FullName })
                .Distinct()
                .ToListAsync(ct);

            var postLines = tenant.Select(p =>
                $"<li><a href=\"/communities/{p.CommunityId}/posts/{p.Slug}\">{p.Title}</a> by {p.AuthorName} in {p.CommunityName} — {p.ReactionCount} reactions</li>");

            var htmlBody = $"""
                <h2>Today's Top Posts</h2>
                <ul>{string.Join("\n", postLines)}</ul>
                <p style="color:#888;font-size:11px">You received this because you are a community member.</p>
                """;

            foreach (var member in members)
            {
                try
                {
                    await emailService.SendAsync(
                        new SendEmailRequest(
                            ToEmail: member.Email,
                            Subject: "KnowHub Daily Community Digest",
                            HtmlBody: htmlBody,
                            ToName: member.FullName
                        ),
                        ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "CommunityDigestService: failed to send digest to {Email}.", member.Email);
                }
            }
        }

        _logger.LogInformation("CommunityDigestService: digest batch complete.");
    }

    private static TimeSpan ComputeDelayUntilNext9Am()
    {
        var now  = DateTime.UtcNow;
        var next = now.Date.AddHours(9);
        if (next <= now) next = next.AddDays(1);
        return next - now;
    }
}
