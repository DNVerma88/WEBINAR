using KnowHub.Application.Contracts.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowHub.Infrastructure.Integrations;

public class StubTeamsNotificationService : ITeamsNotificationService
{
    private readonly TeamsConfiguration _config;
    private readonly ILogger<StubTeamsNotificationService> _logger;

    public StubTeamsNotificationService(
        IOptions<IntegrationsConfiguration> config,
        ILogger<StubTeamsNotificationService> logger)
    {
        _config = config.Value.Teams;
        _logger = logger;
    }

    public Task SendSessionAnnouncementAsync(
        Guid tenantId, string sessionTitle, string sessionUrl,
        string speakerName, DateTime scheduledAt, CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogDebug("Teams integration is disabled. Skipping session announcement for '{Title}'.", sessionTitle);
            return Task.CompletedTask;
        }

        // TODO: Implement real Teams webhook call when IncomingWebhookUrl is configured.
        _logger.LogInformation(
            "Teams: Would announce session '{Title}' by {Speaker} at {Date} to webhook.",
            sessionTitle, speakerName, scheduledAt);

        return Task.CompletedTask;
    }

    public Task SendProposalApprovalAsync(
        Guid tenantId, string proposalTitle, bool isApproved, CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogDebug("Teams integration is disabled. Skipping proposal approval for '{Title}'.", proposalTitle);
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Teams: Would send proposal '{Title}' approval ({Status}) notification.",
            proposalTitle, isApproved ? "Approved" : "Not Approved");

        return Task.CompletedTask;
    }
}
