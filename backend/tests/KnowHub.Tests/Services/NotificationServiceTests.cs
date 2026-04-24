using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class NotificationServiceTests
{
    [Fact]
    public async Task SendAsync_CreatesNotification()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new NotificationService(db, currentUser);

        await service.SendAsync(userId, tenantId, NotificationType.ProposalSubmitted, "Test Title", "Test Body", "Session", Guid.NewGuid());

        var notifications = await service.GetNotificationsAsync(new GetNotificationsRequest(), CancellationToken.None);
        Assert.Equal(1, notifications.TotalCount);
        Assert.Equal("Test Title", notifications.Data[0].Title);
        Assert.False(notifications.Data[0].IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksNotificationRead()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new NotificationService(db, currentUser);

        await service.SendAsync(userId, tenantId, NotificationType.SessionReminder, "Reminder", "Your session is tomorrow");

        var initial = await service.GetNotificationsAsync(new GetNotificationsRequest(), CancellationToken.None);
        var notifId = initial.Data[0].Id;

        await service.MarkAsReadAsync(notifId, CancellationToken.None);

        var updated = await service.GetNotificationsAsync(new GetNotificationsRequest(), CancellationToken.None);
        Assert.True(updated.Data[0].IsRead);
        Assert.NotNull(updated.Data[0].ReadAt);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllRead()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new NotificationService(db, currentUser);

        await service.SendAsync(userId, tenantId, NotificationType.ProposalApproved, "Title1", "Body1");
        await service.SendAsync(userId, tenantId, NotificationType.BadgeAwarded, "Title2", "Body2");

        await service.MarkAllAsReadAsync(CancellationToken.None);

        var result = await service.GetNotificationsAsync(new GetNotificationsRequest { IsRead = true }, CancellationToken.None);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task MarkAsReadAsync_OtherUsersNotification_ThrowsNotFoundException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();

        var senderUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new NotificationService(db, senderUser);
        await service.SendAsync(userId, tenantId, NotificationType.ProposalSubmitted, "Title", "Body");

        var initial = await service.GetNotificationsAsync(new GetNotificationsRequest(), CancellationToken.None);
        var notifId = initial.Data[0].Id;

        var otherUser = new FakeCurrentUserAccessor { UserId = Guid.NewGuid(), TenantId = tenantId, Role = UserRole.Employee };
        var otherService = new NotificationService(db, otherUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            otherService.MarkAsReadAsync(notifId, CancellationToken.None));
    }
}
