using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class KnowledgeRequestServiceTests
{
    private static async Task<(KnowHub.Infrastructure.Persistence.KnowHubDbContext db, Guid tenantId, Guid userId)> SetupAsync(UserRole role = UserRole.Employee)
        => await TestDbFactory.CreateWithSeedAsync(role);

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesKnowledgeRequest()
    {
        var (db, tenantId, userId) = await SetupAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new KnowledgeRequestService(db, currentUser, new FakeNotificationService(), new FakeXpService());

        var result = await service.CreateAsync(new CreateKnowledgeRequestRequest
        {
            Title = "How does Kubernetes work?",
            Description = "Need a deep-dive on K8s internals",
            BountyXp = 50
        }, CancellationToken.None);

        Assert.Equal("How does Kubernetes work?", result.Title);
        Assert.Equal(KnowledgeRequestStatus.Open, result.Status);
        Assert.Equal(0, result.UpvoteCount);
        Assert.False(result.IsAddressed);
        Assert.Equal(50, result.BountyXp);
        Assert.Equal(userId, result.RequesterId);
    }

    [Fact]
    public async Task CreateAsync_NegativeBountyXp_ThrowsBusinessRuleException()
    {
        var (db, tenantId, userId) = await SetupAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new KnowledgeRequestService(db, currentUser, new FakeNotificationService(), new FakeXpService());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(new CreateKnowledgeRequestRequest
            {
                Title = "Bad request",
                Description = "desc",
                BountyXp = -10
            }, CancellationToken.None));
    }

    [Fact]
    public async Task UpvoteAsync_NewUpvote_IncrementsUpvoteCount()
    {
        var (db, tenantId, userId) = await SetupAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new KnowledgeRequestService(db, currentUser, new FakeNotificationService(), new FakeXpService());

        var created = await service.CreateAsync(new CreateKnowledgeRequestRequest
        {
            Title = "I want to learn React",
            Description = "Please make a React session",
            BountyXp = 0
        }, CancellationToken.None);

        // A second user upvotes
        var voterId = Guid.NewGuid();
        var voter = new User
        {
            Id = voterId,
            TenantId = tenantId,
            FullName = "Voter",
            Email = "voter@knowhub.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Employee,
            IsActive = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        db.Users.Add(voter);
        await db.SaveChangesAsync();

        var voterCurrentUser = FakeCurrentUserAccessor.AsEmployee(voterId, tenantId);
        var voterService = new KnowledgeRequestService(db, voterCurrentUser, new FakeNotificationService(), new FakeXpService());

        var result = await voterService.UpvoteAsync(created.Id, CancellationToken.None);

        Assert.Equal(1, result.UpvoteCount);
        Assert.True(result.HasUpvoted);
    }

    [Fact]
    public async Task UpvoteAsync_DuplicateUpvote_ThrowsConflictException()
    {
        var (db, tenantId, userId) = await SetupAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new KnowledgeRequestService(db, currentUser, new FakeNotificationService(), new FakeXpService());

        var created = await service.CreateAsync(new CreateKnowledgeRequestRequest
        {
            Title = "Docker deep-dive",
            Description = "Please",
            BountyXp = 0
        }, CancellationToken.None);

        await service.UpvoteAsync(created.Id, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpvoteAsync(created.Id, CancellationToken.None));
    }

    [Fact]
    public async Task UpvoteAsync_NonExistentRequest_ThrowsNotFoundException()
    {
        var (db, tenantId, userId) = await SetupAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new KnowledgeRequestService(db, currentUser, new FakeNotificationService(), new FakeXpService());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpvoteAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_InvalidCategoryId_ThrowsNotFoundException()
    {
        var (db, tenantId, userId) = await SetupAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new KnowledgeRequestService(db, currentUser, new FakeNotificationService(), new FakeXpService());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(new CreateKnowledgeRequestRequest
            {
                Title = "Bad category",
                Description = "desc",
                CategoryId = Guid.NewGuid(),
                BountyXp = 0
            }, CancellationToken.None));
    }

    [Fact]
    public async Task GetRequestsAsync_WithStatusFilter_ReturnsMatchingRequests()
    {
        var (db, tenantId, userId) = await SetupAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var service = new KnowledgeRequestService(db, currentUser, new FakeNotificationService(), new FakeXpService());

        await service.CreateAsync(new CreateKnowledgeRequestRequest { Title = "Open req", Description = "desc", BountyXp = 0 }, CancellationToken.None);

        var result = await service.GetRequestsAsync(new GetKnowledgeRequestsRequest
        {
            Status = KnowledgeRequestStatus.Open
        }, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Data, r => Assert.Equal(KnowledgeRequestStatus.Open, r.Status));
    }
}

