namespace KnowHub.Application.Contracts.Integrations;

public interface ICalendarIntegrationService
{
    Task CreateCalendarEventAsync(Guid sessionId, string title, DateTime startTime, DateTime endTime, string? location, IEnumerable<string> attendeeEmails, CancellationToken cancellationToken);
    Task UpdateCalendarEventAsync(Guid sessionId, string title, DateTime startTime, DateTime endTime, string? location, CancellationToken cancellationToken);
    Task DeleteCalendarEventAsync(Guid sessionId, CancellationToken cancellationToken);
}
