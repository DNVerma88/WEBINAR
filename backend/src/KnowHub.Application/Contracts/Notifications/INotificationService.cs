using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(GetNotificationsRequest request, CancellationToken cancellationToken);
    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken);
    Task SendAsync(Guid userId, Guid tenantId, Domain.Enums.NotificationType type, string title, string body, string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a batch of notifications in a single <c>SaveChangesAsync</c> call,
    /// avoiding the N×1 round-trip pattern of calling <see cref="SendAsync"/> in a loop.
    /// </summary>
    Task SendBulkAsync(IEnumerable<(Guid UserId, Guid TenantId, Domain.Enums.NotificationType Type, string Title, string Body, string? RelatedEntityType, Guid? RelatedEntityId)> notifications, CancellationToken cancellationToken = default);
}
