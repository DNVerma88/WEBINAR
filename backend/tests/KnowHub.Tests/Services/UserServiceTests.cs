using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new UserService(db, currentUser);

        var result = await service.GetUserByIdAsync(userId, CancellationToken.None);

        Assert.Equal(userId, result.Id);
        Assert.Equal("Test User", result.FullName);
        Assert.Equal("test@knowhub.com", result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistentUser_ThrowsNotFoundException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new UserService(db, currentUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetUserByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUserAsync_OwnProfile_UpdatesSuccessfully()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new UserService(db, currentUser);

        var existing = await service.GetUserByIdAsync(userId, CancellationToken.None);
        var request = new UpdateUserRequest
        {
            FullName = "Updated Name",
            Department = "Engineering",
            Designation = "Senior Dev",
            YearsOfExperience = 5,
            RecordVersion = existing.RecordVersion
        };

        var result = await service.UpdateUserAsync(userId, request, CancellationToken.None);

        Assert.Equal("Updated Name", result.FullName);
        Assert.Equal("Engineering", result.Department);
        Assert.Equal("Senior Dev", result.Designation);
    }

    [Fact]
    public async Task UpdateUserAsync_OtherUserAsEmployee_ThrowsForbiddenException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);

        var otherUserId = Guid.NewGuid();
        var otherUser = new Domain.Entities.User
        {
            Id = otherUserId,
            TenantId = tenantId,
            FullName = "Other User",
            Email = "other@knowhub.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Employee,
            IsActive = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        db.Users.Add(otherUser);
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new UserService(db, currentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdateUserAsync(otherUserId, new UpdateUserRequest { FullName = "Hack", RecordVersion = 1 }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUserAsync_StaleRecordVersion_ThrowsConflictException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new UserService(db, currentUser);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateUserAsync(userId, new UpdateUserRequest { FullName = "Test", RecordVersion = 999 }, CancellationToken.None));
    }

    [Fact]
    public async Task FollowUserAsync_ValidTarget_CreatesFollowRelationship()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);

        var targetId = Guid.NewGuid();
        var targetUser = new Domain.Entities.User
        {
            Id = targetId,
            TenantId = tenantId,
            FullName = "Target User",
            Email = "target@knowhub.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Contributor,
            IsActive = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        db.Users.Add(targetUser);
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new UserService(db, currentUser);

        await service.FollowUserAsync(targetId, CancellationToken.None);

        var follows = db.UserFollowers.Any(f => f.FollowerId == userId && f.FollowedId == targetId);
        Assert.True(follows);
    }

    [Fact]
    public async Task FollowUserAsync_Self_ThrowsBusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new UserService(db, currentUser);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.FollowUserAsync(userId, CancellationToken.None));
    }

    [Fact]
    public async Task GetUsersAsync_WithSearchTerm_FiltersResults()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var service = new UserService(db, currentUser);

        var result = await service.GetUsersAsync(new GetUsersRequest { SearchTerm = "Test" }, CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal("Test User", result.Data[0].FullName);
    }
}
