using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class SessionProposalServiceTests
{
    private static async Task<(KnowHub.Infrastructure.Persistence.KnowHubDbContext db, Guid tenantId, Guid userId, Guid categoryId)> SetupAsync(UserRole role = UserRole.Contributor)
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(role);

        var category = new Domain.Entities.Category
        {
            TenantId = tenantId,
            Name = "Engineering",
            IsActive = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return (db, tenantId, userId, category.Id);
    }

    private static CreateSessionProposalRequest BuildRequest(Guid categoryId) => new()
    {
        Title = "Intro to Clean Architecture",
        CategoryId = categoryId,
        Topic = "Architecture patterns",
        Description = "Deep dive into Clean Architecture",
        Format = SessionFormat.Webinar,
        EstimatedDurationMinutes = 60,
        DifficultyLevel = DifficultyLevel.Intermediate,
        AllowRecording = true
    };

    [Fact]
    public async Task CreateAsync_ContributorRole_CreatesProposalAsDraft()
    {
        var (db, tenantId, userId, categoryId) = await SetupAsync(UserRole.Contributor);
        var currentUser = FakeCurrentUserAccessor.AsContributor(userId, tenantId);
        var notifications = new FakeNotificationService();
        var service = new SessionProposalService(db, currentUser, notifications);

        var result = await service.CreateAsync(BuildRequest(categoryId), CancellationToken.None);

        Assert.Equal(ProposalStatus.Draft, result.Status);
        Assert.Equal("Intro to Clean Architecture", result.Title);
        Assert.Equal(userId, result.ProposerId);
    }

    [Fact]
    public async Task CreateAsync_EmployeeRole_ThrowsForbiddenException()
    {
        var (db, tenantId, userId, categoryId) = await SetupAsync(UserRole.Employee);
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var notifications = new FakeNotificationService();
        var service = new SessionProposalService(db, currentUser, notifications);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateAsync(BuildRequest(categoryId), CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAsync_DraftProposal_ChangesStatusToSubmitted()
    {
        var (db, tenantId, userId, categoryId) = await SetupAsync(UserRole.Contributor);
        var currentUser = FakeCurrentUserAccessor.AsContributor(userId, tenantId);
        var notifications = new FakeNotificationService();
        var service = new SessionProposalService(db, currentUser, notifications);

        var proposal = await service.CreateAsync(BuildRequest(categoryId), CancellationToken.None);
        var result = await service.SubmitAsync(proposal.Id, CancellationToken.None);

        Assert.Equal(ProposalStatus.Submitted, result.Status);
        Assert.NotNull(result.SubmittedAt);
    }

    [Fact]
    public async Task SubmitAsync_DifferentUser_ThrowsForbiddenException()
    {
        var (db, tenantId, userId, categoryId) = await SetupAsync(UserRole.Contributor);
        var ownerUser = FakeCurrentUserAccessor.AsContributor(userId, tenantId);
        var notifications = new FakeNotificationService();
        var service = new SessionProposalService(db, ownerUser, notifications);
        var proposal = await service.CreateAsync(BuildRequest(categoryId), CancellationToken.None);

        var otherId = Guid.NewGuid();
        var otherUser = FakeCurrentUserAccessor.AsContributor(otherId, tenantId);
        var otherService = new SessionProposalService(db, otherUser, notifications);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            otherService.SubmitAsync(proposal.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ApproveAsync_ManagerRole_TransitionsToKnowledgeTeamReview()
    {
        var (db, tenantId, userId, categoryId) = await SetupAsync(UserRole.Contributor);
        var ownerUser = FakeCurrentUserAccessor.AsContributor(userId, tenantId);
        var notifications = new FakeNotificationService();
        var contributorService = new SessionProposalService(db, ownerUser, notifications);
        var proposal = await contributorService.CreateAsync(BuildRequest(categoryId), CancellationToken.None);
        await contributorService.SubmitAsync(proposal.Id, CancellationToken.None);

        var managerId = Guid.NewGuid();
        var managerUser = FakeCurrentUserAccessor.AsManager(managerId, tenantId);
        var managerService = new SessionProposalService(db, managerUser, notifications);
        var result = await managerService.ApproveAsync(proposal.Id, new ApproveProposalRequest { Comment = "Looks good!" }, CancellationToken.None);

        Assert.Equal(ProposalStatus.KnowledgeTeamReview, result.Status);
        Assert.Equal(2, notifications.SentNotifications.Count); // 1 from Submit, 1 from Approve
    }

    [Fact]
    public async Task RejectAsync_SubmittedProposal_ChangesStatusToRejected()
    {
        var (db, tenantId, userId, categoryId) = await SetupAsync(UserRole.Contributor);
        var ownerUser = FakeCurrentUserAccessor.AsContributor(userId, tenantId);
        var notifications = new FakeNotificationService();
        var contributorService = new SessionProposalService(db, ownerUser, notifications);
        var proposal = await contributorService.CreateAsync(BuildRequest(categoryId), CancellationToken.None);
        await contributorService.SubmitAsync(proposal.Id, CancellationToken.None);

        var managerId = Guid.NewGuid();
        var managerUser = FakeCurrentUserAccessor.AsManager(managerId, tenantId);
        var managerService = new SessionProposalService(db, managerUser, notifications);
        var result = await managerService.RejectAsync(proposal.Id, new RejectProposalRequest { Comment = "Needs more clarity." }, CancellationToken.None);

        Assert.Equal(ProposalStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ThrowsNotFoundException()
    {
        var (db, tenantId, userId, _) = await SetupAsync();
        var currentUser = FakeCurrentUserAccessor.AsContributor(userId, tenantId);
        var notifications = new FakeNotificationService();
        var service = new SessionProposalService(db, currentUser, notifications);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
