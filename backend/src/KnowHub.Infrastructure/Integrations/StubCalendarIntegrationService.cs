using KnowHub.Application.Contracts.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowHub.Infrastructure.Integrations;

public class StubCalendarIntegrationService : ICalendarIntegrationService
{
    private readonly OutlookCalendarConfiguration _config;
    private readonly ILogger<StubCalendarIntegrationService> _logger;

    public StubCalendarIntegrationService(
        IOptions<IntegrationsConfiguration> config,
        ILogger<StubCalendarIntegrationService> logger)
    {
        _config = config.Value.OutlookCalendar;
        _logger = logger;
    }

    public Task CreateCalendarEventAsync(
        Guid sessionId, string title, DateTime startTime, DateTime endTime,
        string? location, IEnumerable<string> attendeeEmails, CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogDebug("Calendar integration is disabled. Skipping create event for '{Title}'.", title);
            return Task.CompletedTask;
        }

        // TODO: Implement real Microsoft Graph API call when ClientId/ClientSecret are configured.
        _logger.LogInformation(
            "Calendar: Would create event '{Title}' from {Start} to {End} for {Count} attendees.",
            title, startTime, endTime, attendeeEmails.Count());

        return Task.CompletedTask;
    }

    public Task UpdateCalendarEventAsync(
        Guid sessionId, string title, DateTime startTime, DateTime endTime,
        string? location, CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogDebug("Calendar integration is disabled. Skipping update event for '{Title}'.", title);
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Calendar: Would update event '{Title}' ({SessionId}) to {Start}–{End}.",
            title, sessionId, startTime, endTime);

        return Task.CompletedTask;
    }

    public Task DeleteCalendarEventAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogDebug("Calendar integration is disabled. Skipping delete event for session {SessionId}.", sessionId);
            return Task.CompletedTask;
        }

        _logger.LogInformation("Calendar: Would delete calendar event for session {SessionId}.", sessionId);

        return Task.CompletedTask;
    }
}
