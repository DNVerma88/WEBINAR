using KnowHub.Api.Hubs;
using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using Microsoft.AspNetCore.SignalR;

namespace KnowHub.Api.Infrastructure;

/// <summary>
/// Delivers real-time notifications to connected SignalR clients.
/// Registered in the API layer so it can reference the Hub without
/// creating a circular dependency into KnowHub.Infrastructure.
/// </summary>
public sealed class SignalRNotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationPusher(IHubContext<NotificationHub> hubContext)
        => _hubContext = hubContext;

    public Task PushToUserAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default)
        => _hubContext.Clients
                      .User(userId.ToString())
                      .SendAsync("ReceiveNotification", notification, cancellationToken);

    public Task PushToTenantAsync(Guid tenantId, NotificationDto notification, CancellationToken cancellationToken = default)
        => _hubContext.Clients
                      .Group($"tenant:{tenantId}")
                      .SendAsync("ReceiveNotification", notification, cancellationToken);
}
