using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace KnowHub.Api.Hubs;

/// <summary>
/// Real-time notification hub. JWT is passed via query-string "access_token" for
/// WebSocket connections (configured in JwtBearerEvents.OnMessageReceived).
/// On connect the user is added to a tenant group for broadcast notifications.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirstValue("tenantId");
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = Context.User?.FindFirstValue("tenantId");
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");

        await base.OnDisconnectedAsync(exception);
    }
}
