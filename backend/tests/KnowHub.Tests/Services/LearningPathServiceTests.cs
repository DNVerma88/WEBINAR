using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class LearningPathServiceTests
{
    private sealed class NoOpXpService : IXpService
    {
        public Task<UserXpDto> GetUserXpAsync(Guid userId, CancellationToken ct)
            => Task.FromResult(new UserXpDto { UserId = userId });

        public Task AwardXpAsync(AwardXpRequest request, CancellationToken ct)
            => Task.CompletedTask;
    }

    private static LearningPathService CreateService(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        ICurrentUserAccessor currentUser)
        => new(db, currentUser, new NoOpXpService());

    [Fact]
    public async Task GetPathsAsync_EmployeeRole_HidesUnpublishedPaths()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);

        db.LearningPaths.AddRange(
            new LearningPath { TenantId = tenantId, Title = "Published Path", Slug = "published-path", IsPublished = true, CreatedBy = userId, ModifiedBy = userId },
            new LearningPath { TenantId = tenantId, Title = "Draft Path", Slug = "draft-path", IsPublished = false, CreatedBy = userId, ModifiedBy = userId }
        );
        await db.SaveChangesAsync();

        var result = await service.GetPathsAsync(new GetLearningPathsRequest(), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Published Path", result.Data[0].Title);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesLearningPath()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.KnowledgeTeam);
        var currentUser = FakeCurrentUserAccessor.AsKnowledgeTeam(userId, tenantId);
        var service = CreateService(db, currentUser);

        var result = await service.CreateAsync(new CreateLearningPathRequest
        {
            Title = "React Mastery Path",
            Description = "From zero to hero with React",
            Objective = "Master React 19",
            EstimatedDurationMinutes = 240
        }, CancellationToken.None);

        Assert.Equal("React Mastery Path", result.Title);
        Assert.Contains("react-mastery-path", result.Slug);
        Assert.False(result.IsPublished);
    }

    [Fact]
    public async Task CreateAsync_EmployeeRole_ThrowsForbiddenException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateAsync(new CreateLearningPathRequest
            {
                Title = "Unauthorized Path",
                Description = "desc",
                Objective = "obj"
            }, CancellationToken.None));
    }

    [Fact]
    public async Task EnrolAsync_ValidPath_UsersEnrolmentIsCreated()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);

        var path = new LearningPath
        {
            TenantId = tenantId,
            Title = "K8s Learning Path",
            Slug = "k8s-learning-path",
            IsPublished = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        db.LearningPaths.Add(path);
        await db.SaveChangesAsync();

        await service.EnrolAsync(path.Id, CancellationToken.None);

        var progress = await service.GetProgressAsync(path.Id, CancellationToken.None);
        Assert.Equal(path.Id, progress.LearningPathId);
        Assert.Equal(userId, progress.UserId);
    }

    [Fact]
    public async Task EnrolAsync_AlreadyEnrolled_ThrowsConflictException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);

        var path = new LearningPath
        {
            TenantId = tenantId,
            Title = "Duplicate Enrolment Path",
            Slug = "dup-enrol-path",
            IsPublished = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        db.LearningPaths.Add(path);
        await db.SaveChangesAsync();

        await service.EnrolAsync(path.Id, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.EnrolAsync(path.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_UnpublishedPath_ThrowsForbiddenExceptionForEmployee()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);

        var path = new LearningPath
        {
            TenantId = tenantId,
            Title = "Secret Draft",
            Slug = "secret-draft",
            IsPublished = false,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        db.LearningPaths.Add(path);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetByIdAsync(path.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetProgressAsync_NotEnrolled_ThrowsNotFoundException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = CreateService(db, currentUser);

        var path = new LearningPath
        {
            TenantId = tenantId,
            Title = "Not Enrolled Path",
            Slug = "not-enrolled",
            IsPublished = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        db.LearningPaths.Add(path);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetProgressAsync(path.Id, CancellationToken.None));
    }
}
