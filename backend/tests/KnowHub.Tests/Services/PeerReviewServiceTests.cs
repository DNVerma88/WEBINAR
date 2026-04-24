using KnowHub.Application.Contracts.PeerReview;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class PeerReviewServiceTests
{
    private static PeerReviewService CreateService(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        KnowHub.Application.Contracts.ICurrentUserAccessor currentUser)
        => new(db, currentUser, new FakeNotificationService());

    private static async Task<(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        Guid tenantId,
        Guid nominatorId,
        Guid reviewerId,
        Guid assetId)> SetupAsync()
    {
        var (db, tenantId, nominatorId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Contributor);

        var reviewerId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = reviewerId, TenantId = tenantId, FullName = "Reviewer", Email = "reviewer@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@123"), Role = UserRole.Contributor,
            IsActive = true, CreatedBy = reviewerId, ModifiedBy = reviewerId,
        });

        var assetId = Guid.NewGuid();
        db.KnowledgeAssets.Add(new KnowledgeAsset
        {
            Id = assetId, TenantId = tenantId, Title = "Test Asset",
            AssetType = KnowledgeAssetType.Documentation, Url = "https://example.com/doc.pdf",
            IsVerified = false,
            CreatedBy = nominatorId, ModifiedBy = nominatorId,
        });

        await db.SaveChangesAsync();
        return (db, tenantId, nominatorId, reviewerId, assetId);
    }

    // -- NominateReviewer -----------------------------------------------------

    [Fact]
    public async Task NominateReviewerAsync_ValidRequest_CreatesReview()
    {
        var (db, tenantId, nominatorId, reviewerId, assetId) = await SetupAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(nominatorId, tenantId);
        var service = CreateService(db, currentUser);

        var result = await service.NominateReviewerAsync(
            new NominateReviewerRequest(assetId, reviewerId),
            CancellationToken.None);

        Assert.Equal(AssetReviewStatus.Pending, result.Status);
        Assert.Equal(assetId, result.KnowledgeAssetId);
        Assert.Equal(reviewerId, result.ReviewerId);
    }

    [Fact]
    public async Task NominateReviewerAsync_SelfNomination_ThrowsBusinessRuleException()
    {
        var (db, tenantId, nominatorId, _, assetId) = await SetupAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(nominatorId, tenantId);
        var service = CreateService(db, currentUser);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.NominateReviewerAsync(
                new NominateReviewerRequest(assetId, nominatorId), // same as current user
                CancellationToken.None));
    }

    [Fact]
    public async Task NominateReviewerAsync_AlreadyVerified_ThrowsBusinessRuleException()
    {
        var (db, tenantId, nominatorId, reviewerId, assetId) = await SetupAsync();

        var asset = await db.KnowledgeAssets.FindAsync(assetId);
        asset!.IsVerified = true;
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsEmployee(nominatorId, tenantId);
        var service = CreateService(db, currentUser);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.NominateReviewerAsync(
                new NominateReviewerRequest(assetId, reviewerId),
                CancellationToken.None));
    }

    [Fact]
    public async Task NominateReviewerAsync_AssetNotFound_ThrowsNotFoundException()
    {
        var (db, tenantId, nominatorId, reviewerId, _) = await SetupAsync();
        var currentUser = FakeCurrentUserAccessor.AsEmployee(nominatorId, tenantId);
        var service = CreateService(db, currentUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.NominateReviewerAsync(
                new NominateReviewerRequest(Guid.NewGuid(), reviewerId),
                CancellationToken.None));
    }

    // -- SubmitReview ---------------------------------------------------------

    [Fact]
    public async Task SubmitReviewAsync_Approve_SetsAssetVerified()
    {
        var (db, tenantId, nominatorId, reviewerId, assetId) = await SetupAsync();

        // Create pending review
        var reviewId = Guid.NewGuid();
        db.KnowledgeAssetReviews.Add(new KnowledgeAssetReview
        {
            Id = reviewId, TenantId = tenantId, KnowledgeAssetId = assetId,
            ReviewerId = reviewerId, NominatedByUserId = nominatorId,
            NominatedAt = DateTime.UtcNow, Status = AssetReviewStatus.Pending,
            CreatedBy = nominatorId, ModifiedBy = nominatorId,
        });
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsEmployee(reviewerId, tenantId);
        var service = CreateService(db, currentUser);

        var result = await service.SubmitReviewAsync(reviewId,
            new SubmitReviewRequest(AssetReviewStatus.Approved, "Looks good!"),
            CancellationToken.None);

        Assert.Equal(AssetReviewStatus.Approved, result.Status);
        Assert.Equal("Looks good!", result.Comments);

        // Verify asset is now marked as verified
        var asset = await db.KnowledgeAssets.FindAsync(assetId);
        Assert.True(asset!.IsVerified);
        Assert.Equal(reviewerId, asset.VerifiedById);
    }

    [Fact]
    public async Task SubmitReviewAsync_NotTheReviewer_ThrowsForbiddenException()
    {
        var (db, tenantId, nominatorId, reviewerId, assetId) = await SetupAsync();

        var reviewId = Guid.NewGuid();
        db.KnowledgeAssetReviews.Add(new KnowledgeAssetReview
        {
            Id = reviewId, TenantId = tenantId, KnowledgeAssetId = assetId,
            ReviewerId = reviewerId, NominatedByUserId = nominatorId,
            NominatedAt = DateTime.UtcNow, Status = AssetReviewStatus.Pending,
            CreatedBy = nominatorId, ModifiedBy = nominatorId,
        });
        await db.SaveChangesAsync();

        // nominatorId tries to submit the review (not the reviewer)
        var currentUser = FakeCurrentUserAccessor.AsEmployee(nominatorId, tenantId);
        var service = CreateService(db, currentUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.SubmitReviewAsync(reviewId,
                new SubmitReviewRequest(AssetReviewStatus.Rejected, null),
                CancellationToken.None));
    }

    // -- GetPendingReviews ----------------------------------------------------

    [Fact]
    public async Task GetPendingReviewsAsync_ReturnsOnlyCurrentUsersReviews()
    {
        var (db, tenantId, nominatorId, reviewerId, assetId) = await SetupAsync();

        db.KnowledgeAssetReviews.Add(new KnowledgeAssetReview
        {
            TenantId = tenantId, KnowledgeAssetId = assetId,
            ReviewerId = reviewerId, NominatedByUserId = nominatorId,
            NominatedAt = DateTime.UtcNow, Status = AssetReviewStatus.Pending,
            CreatedBy = nominatorId, ModifiedBy = nominatorId,
        });
        await db.SaveChangesAsync();

        // Reviewer queries pending reviews
        var currentUser = FakeCurrentUserAccessor.AsEmployee(reviewerId, tenantId);
        var service = CreateService(db, currentUser);

        var result = await service.GetPendingReviewsAsync(new GetPendingReviewsRequest(), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(reviewerId, result.Data[0].ReviewerId);
    }
}
