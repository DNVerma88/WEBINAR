using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;

namespace KnowHub.Tests.TestHelpers;

public class FakeNotificationService : INotificationService
{
    public List<SentNotification> SentNotifications { get; } = new();

    public Task<Application.Models.PagedResult<NotificationDto>> GetNotificationsAsync(GetNotificationsRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new Application.Models.PagedResult<NotificationDto> { Data = new List<NotificationDto>(), TotalCount = 0, PageNumber = 1, PageSize = 20 });

    public Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task MarkAllAsReadAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SendAsync(Guid userId, Guid tenantId, NotificationType type, string title, string body,
        string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken cancellationToken = default)
    {
        SentNotifications.Add(new SentNotification(userId, tenantId, type, title, body, relatedEntityType, relatedEntityId));
        return Task.CompletedTask;
    }
}

public record SentNotification(Guid UserId, Guid TenantId, NotificationType Type, string Title, string Body, string? RelatedEntityType, Guid? RelatedEntityId);
