using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class XpServiceTests
{
    [Fact]
    public async Task GetUserXpAsync_UserNotFound_ThrowsNotFoundException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new XpService(db, currentUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetUserXpAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task GetUserXpAsync_NoEvents_ReturnsTotalXpOfZero()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new XpService(db, currentUser);

        var result = await service.GetUserXpAsync(userId, CancellationToken.None);

        Assert.Equal(0, result.TotalXp);
        Assert.Empty(result.RecentEvents);
    }

    [Fact]
    public async Task AwardXpAsync_AttendSession_CreatesEventWithTenXp()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new XpService(db, currentUser);

        await service.AwardXpAsync(new AwardXpRequest
        {
            UserId = userId,
            TenantId = tenantId,
            EventType = XpEventType.AttendSession
        }, CancellationToken.None);

        var result = await service.GetUserXpAsync(userId, CancellationToken.None);

        Assert.Equal(10, result.TotalXp);
        Assert.Single(result.RecentEvents);
        Assert.Equal(XpEventType.AttendSession, result.RecentEvents[0].EventType);
    }

    [Fact]
    public async Task AwardXpAsync_DeliverSession_CreatesEventWithFiftyXp()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new XpService(db, currentUser);

        await service.AwardXpAsync(new AwardXpRequest
        {
            UserId = userId,
            TenantId = tenantId,
            EventType = XpEventType.DeliverSession
        }, CancellationToken.None);

        var result = await service.GetUserXpAsync(userId, CancellationToken.None);

        Assert.Equal(50, result.TotalXp);
    }

    [Fact]
    public async Task GetUserXpAsync_MultipleEvents_ReturnsSummedTotalXp()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new XpService(db, currentUser);

        await service.AwardXpAsync(new AwardXpRequest { UserId = userId, TenantId = tenantId, EventType = XpEventType.AttendSession }, CancellationToken.None);
        await service.AwardXpAsync(new AwardXpRequest { UserId = userId, TenantId = tenantId, EventType = XpEventType.UploadAsset }, CancellationToken.None);

        var result = await service.GetUserXpAsync(userId, CancellationToken.None);

        // AttendSession=10, UploadAsset=10
        Assert.Equal(20, result.TotalXp);
        Assert.Equal(2, result.RecentEvents.Count);
    }
}
