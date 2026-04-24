using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class CommunityServiceTests
{
    [Fact]
    public async Task CreateAsync_AdminRole_CreatesCommunity()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var service = new CommunityService(db, currentUser);

        var result = await service.CreateAsync(new CreateCommunityRequest { Name = "AI Community", Description = "AI discussions" }, CancellationToken.None);

        Assert.Equal("AI Community", result.Name);
        Assert.Equal("ai-community", result.Slug);
        Assert.Equal(0, result.MemberCount);
    }

    [Fact]
    public async Task CreateAsync_EmployeeRole_ThrowsForbiddenException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new CommunityService(db, currentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateAsync(new CreateCommunityRequest { Name = "Test" }, CancellationToken.None));
    }

    [Fact]
    public async Task JoinAsync_NotMember_JoinsCommunity()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var adminUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var adminService = new CommunityService(db, adminUser);
        var community = await adminService.CreateAsync(new CreateCommunityRequest { Name = "DevOps" }, CancellationToken.None);

        var (db2, _, memberId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee, db.Database.ProviderName);
        // Use same db instance
        var memberUser = FakeCurrentUserAccessor.AsEmployee(memberId, tenantId);
        var memberService = new CommunityService(db, memberUser);

        await memberService.JoinAsync(community.Id, CancellationToken.None);

        var updated = await memberService.GetByIdAsync(community.Id, CancellationToken.None);
        Assert.Equal(1, updated.MemberCount);
        Assert.True(updated.IsMember);
    }

    [Fact]
    public async Task JoinAsync_AlreadyMember_ThrowsConflictException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var service = new CommunityService(db, currentUser);
        var community = await service.CreateAsync(new CreateCommunityRequest { Name = "QA" }, CancellationToken.None);

        await service.JoinAsync(community.Id, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.JoinAsync(community.Id, CancellationToken.None));
    }

    [Fact]
    public async Task LeaveAsync_Member_ReducesMemberCount()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var service = new CommunityService(db, currentUser);
        var community = await service.CreateAsync(new CreateCommunityRequest { Name = "Security" }, CancellationToken.None);

        await service.JoinAsync(community.Id, CancellationToken.None);
        await service.LeaveAsync(community.Id, CancellationToken.None);

        var updated = await service.GetByIdAsync(community.Id, CancellationToken.None);
        Assert.Equal(0, updated.MemberCount);
        Assert.False(updated.IsMember);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ThrowsNotFoundException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new CommunityService(db, currentUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
