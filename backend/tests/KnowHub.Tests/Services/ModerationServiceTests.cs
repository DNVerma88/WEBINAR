using KnowHub.Application.Contracts.Moderation;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class ModerationServiceTests
{
    private static ModerationService CreateService(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        KnowHub.Application.Contracts.ICurrentUserAccessor currentUser)
        => new(db, currentUser);

    // -- FlagContent ----------------------------------------------------------

    [Fact]
    public async Task FlagContentAsync_ValidRequest_CreatesPendingFlag()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);
        var contentId = Guid.NewGuid();

        var result = await service.FlagContentAsync(
            new FlagContentRequest(FlaggedContentType.Session, contentId, FlagReason.Inappropriate, "Bad content"),
            CancellationToken.None);

        Assert.Equal(FlagStatus.Pending, result.Status);
        Assert.Equal(FlaggedContentType.Session, result.ContentType);
        Assert.Equal(contentId, result.ContentId);
    }

    // -- GetContentFlags ------------------------------------------------------

    [Fact]
    public async Task GetContentFlagsAsync_NonAdmin_ThrowsForbiddenException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetContentFlagsAsync(new GetContentFlagsRequest(null, null), CancellationToken.None));
    }

    [Fact]
    public async Task GetContentFlagsAsync_AdminUser_ReturnsFlags()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);

        // Seed a flag
        db.ContentFlags.Add(new ContentFlag
        {
            TenantId = tenantId,
            FlaggedByUserId = adminId,
            ContentType = FlaggedContentType.KnowledgeAsset,
            ContentId = Guid.NewGuid(),
            Reason = FlagReason.Spam,
            Status = FlagStatus.Pending,
            CreatedBy = adminId,
            ModifiedBy = adminId,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, currentUser);
        var result = await service.GetContentFlagsAsync(new GetContentFlagsRequest(null, null, 1, 20), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(FlagReason.Spam, result.Data[0].Reason);
    }

    // -- ReviewFlag -----------------------------------------------------------

    [Fact]
    public async Task ReviewFlagAsync_AdminReviewsPendingFlag_UpdatesStatus()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);

        var flagId = Guid.NewGuid();
        db.ContentFlags.Add(new ContentFlag
        {
            Id = flagId,
            TenantId = tenantId,
            FlaggedByUserId = adminId,
            ContentType = FlaggedContentType.Session,
            ContentId = Guid.NewGuid(),
            Reason = FlagReason.Inaccurate,
            Status = FlagStatus.Pending,
            CreatedBy = adminId,
            ModifiedBy = adminId,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, currentUser);
        var result = await service.ReviewFlagAsync(flagId,
            new ReviewFlagRequest(FlagStatus.ActionTaken, "Content removed."),
            CancellationToken.None);

        Assert.Equal(FlagStatus.ActionTaken, result.Status);
        Assert.Equal("Content removed.", result.ReviewNotes);
    }

    [Fact]
    public async Task ReviewFlagAsync_NonAdmin_ThrowsForbiddenException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.ReviewFlagAsync(Guid.NewGuid(), new ReviewFlagRequest(FlagStatus.Dismissed, null), CancellationToken.None));
    }

    // -- SuspendUser ----------------------------------------------------------

    [Fact]
    public async Task SuspendUserAsync_AdminSuspendsEmployee_CreatesSuspension()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var targetUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = targetUserId, TenantId = tenantId, FullName = "Target", Email = "target@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ss1"), Role = UserRole.Employee, IsActive = true,
            CreatedBy = targetUserId, ModifiedBy = targetUserId,
        });
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var service = CreateService(db, currentUser);

        var result = await service.SuspendUserAsync(
            new SuspendUserRequest(targetUserId, "Policy violation", null),
            CancellationToken.None);

        Assert.True(result.IsActive);
        Assert.Equal(targetUserId, result.UserId);
        Assert.Equal("Policy violation", result.Reason);
    }

    [Fact]
    public async Task SuspendUserAsync_NonAdmin_ThrowsForbiddenException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.SuspendUserAsync(new SuspendUserRequest(Guid.NewGuid(), "Test", null), CancellationToken.None));
    }

    // -- LiftSuspension -------------------------------------------------------

    [Fact]
    public async Task LiftSuspensionAsync_AdminLiftsActiveSuspension_DeactivatesSuspension()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var targetUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = targetUserId, TenantId = tenantId, FullName = "Target User", Email = "target2@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ss1"), Role = UserRole.Employee, IsActive = false,
            CreatedBy = targetUserId, ModifiedBy = targetUserId,
        });

        var suspensionId = Guid.NewGuid();
        db.UserSuspensions.Add(new UserSuspension
        {
            Id = suspensionId, TenantId = tenantId, UserId = targetUserId,
            SuspendedByUserId = adminId, Reason = "Test", IsActive = true,
            SuspendedAt = DateTime.UtcNow.AddDays(-1),
            CreatedBy = adminId, ModifiedBy = adminId,
        });
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var service = CreateService(db, currentUser);

        var result = await service.LiftSuspensionAsync(suspensionId,
            new LiftSuspensionRequest("Appeal approved"),
            CancellationToken.None);

        Assert.False(result.IsActive);
        Assert.Equal("Appeal approved", result.LiftReason);
    }
}
