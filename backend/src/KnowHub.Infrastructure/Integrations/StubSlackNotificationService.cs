using KnowHub.Application.Contracts.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowHub.Infrastructure.Integrations;

public class StubSlackNotificationService : ISlackNotificationService
{
    private readonly SlackConfiguration _config;
    private readonly ILogger<StubSlackNotificationService> _logger;

    public StubSlackNotificationService(
        IOptions<IntegrationsConfiguration> config,
        ILogger<StubSlackNotificationService> logger)
    {
        _config = config.Value.Slack;
        _logger = logger;
    }

    public Task SendSessionAnnouncementAsync(
        Guid tenantId, string sessionTitle, string sessionUrl,
        string speakerName, DateTime scheduledAt, CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogDebug("Slack integration is disabled. Skipping session announcement for '{Title}'.", sessionTitle);
            return Task.CompletedTask;
        }

        // TODO: Implement real Slack bot API call when BotToken is configured.
        _logger.LogInformation(
            "Slack: Would post session '{Title}' announcement to channel {Channel}.",
            sessionTitle, _config.DefaultChannel);

        return Task.CompletedTask;
    }

    public Task SendProposalApprovalAsync(
        Guid tenantId, string proposalTitle, bool isApproved, CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogDebug("Slack integration is disabled. Skipping proposal approval for '{Title}'.", proposalTitle);
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Slack: Would send proposal '{Title}' approval ({Status}) to channel {Channel}.",
            proposalTitle, isApproved ? "Approved" : "Not Approved", _config.DefaultChannel);

        return Task.CompletedTask;
    }
}
