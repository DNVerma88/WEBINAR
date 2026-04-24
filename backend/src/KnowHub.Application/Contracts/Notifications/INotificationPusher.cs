using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

/// <summary>
/// Pushes real-time notifications to connected clients via SignalR.
/// Implemented in the API layer; optional in Infrastructure so tests can omit it.
/// </summary>
public interface INotificationPusher
{
    Task PushToUserAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default);
    Task PushToTenantAsync(Guid tenantId, NotificationDto notification, CancellationToken cancellationToken = default);
}
