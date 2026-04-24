using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly INotificationPusher? _pusher;

    public NotificationService(KnowHubDbContext db, ICurrentUserAccessor currentUser, INotificationPusher? pusher = null)
    {
        _db = db;
        _currentUser = currentUser;
        _pusher = pusher;
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(GetNotificationsRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Notifications
            .Where(n => n.UserId == _currentUser.UserId && n.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (request.IsRead.HasValue) query = query.Where(n => n.IsRead == request.IsRead.Value);

        var (data, total) = await query
            .OrderByDescending(n => n.CreatedDate)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                NotificationType = n.NotificationType,
                Title = n.Title,
                Body = n.Body,
                RelatedEntityType = n.RelatedEntityType,
                RelatedEntityId = n.RelatedEntityId,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedDate = n.CreatedDate
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResult<NotificationDto> { Data = data, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == _currentUser.UserId && n.TenantId == _currentUser.TenantId, cancellationToken);
        if (notification is null) throw new NotFoundException("Notification", id);

        if (notification.IsRead) return;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        notification.ModifiedOn = DateTime.UtcNow;
        notification.ModifiedBy = _currentUser.UserId;
        notification.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        // API-24: bulk UPDATE via ExecuteUpdateAsync — avoids loading every unread notification
        // into memory and emitting individual UPDATE statements per row through the change tracker
        await _db.Notifications
            .Where(n => n.UserId == _currentUser.UserId && n.TenantId == _currentUser.TenantId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now)
                .SetProperty(n => n.ModifiedOn, now),
                cancellationToken);
    }

    public async Task SendAsync(
        Guid userId,
        Guid tenantId,
        NotificationType type,
        string title,
        string body,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default)
    {
        var systemUserId = userId;
        var notification = new Notification
        {
            TenantId = tenantId,
            UserId = userId,
            NotificationType = type,
            Title = title,
            Body = body,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            CreatedBy = systemUserId,
            ModifiedBy = systemUserId
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);

        if (_pusher is not null)
        {
            var dto = new NotificationDto
            {
                Id = notification.Id,
                NotificationType = notification.NotificationType,
                Title = notification.Title,
                Body = notification.Body,
                RelatedEntityType = notification.RelatedEntityType,
                RelatedEntityId = notification.RelatedEntityId,
                IsRead = false,
                ReadAt = null,
                CreatedDate = notification.CreatedDate,
            };

            // Fire-and-forget: don't let a SignalR failure abort the business operation
            _ = _pusher.PushToUserAsync(userId, dto, cancellationToken);
        }
    }

    public async Task SendBulkAsync(
        IEnumerable<(Guid UserId, Guid TenantId, NotificationType Type, string Title, string Body, string? RelatedEntityType, Guid? RelatedEntityId)> notifications,
        CancellationToken cancellationToken = default)
    {
        var entities = notifications.Select(n =>
        {
            var entity = new Notification
            {
                TenantId = n.TenantId,
                UserId = n.UserId,
                NotificationType = n.Type,
                Title = n.Title,
                Body = n.Body,
                RelatedEntityType = n.RelatedEntityType,
                RelatedEntityId = n.RelatedEntityId,
                IsRead = false,
                CreatedBy = n.UserId,
                ModifiedBy = n.UserId
            };
            return entity;
        }).ToList();

        _db.Notifications.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);

        // Push real-time notifications after the DB write succeeds
        if (_pusher is not null)
        {
            foreach (var (entity, source) in entities.Zip(notifications))
            {
                var dto = new NotificationDto
                {
                    Id = entity.Id,
                    NotificationType = entity.NotificationType,
                    Title = entity.Title,
                    Body = entity.Body,
                    RelatedEntityType = entity.RelatedEntityType,
                    RelatedEntityId = entity.RelatedEntityId,
                    IsRead = false,
                    ReadAt = null,
                    CreatedDate = entity.CreatedDate,
                };
                _ = _pusher.PushToUserAsync(source.UserId, dto, cancellationToken);
            }
        }
    }
}
