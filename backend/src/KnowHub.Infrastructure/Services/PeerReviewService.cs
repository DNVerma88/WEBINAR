using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.PeerReview;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class PeerReviewService : IPeerReviewService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly INotificationService _notificationService;

    public PeerReviewService(
        KnowHubDbContext db,
        ICurrentUserAccessor currentUser,
        INotificationService notificationService)
    {
        _db = db;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<AssetReviewDto> NominateReviewerAsync(
        NominateReviewerRequest request, CancellationToken cancellationToken)
    {
        var asset = await _db.KnowledgeAssets
            .FirstOrDefaultAsync(a => a.Id == request.KnowledgeAssetId && a.TenantId == _currentUser.TenantId,
                cancellationToken);

        if (asset is null) throw new NotFoundException("KnowledgeAsset", request.KnowledgeAssetId);

        if (asset.IsVerified)
            throw new BusinessRuleException("This knowledge asset is already verified.");

        var reviewer = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.ReviewerId && u.TenantId == _currentUser.TenantId,
                cancellationToken);

        if (reviewer is null) throw new NotFoundException("User", request.ReviewerId);

        if (request.ReviewerId == _currentUser.UserId)
            throw new BusinessRuleException("You cannot nominate yourself as a reviewer.");

        var existing = await _db.KnowledgeAssetReviews
            .AnyAsync(r => r.KnowledgeAssetId == request.KnowledgeAssetId
                && r.ReviewerId == request.ReviewerId
                && r.Status == AssetReviewStatus.Pending
                && r.TenantId == _currentUser.TenantId,
                cancellationToken);

        if (existing)
            throw new ConflictException("This reviewer already has a pending review for this asset.");

        var review = new KnowledgeAssetReview
        {
            TenantId = _currentUser.TenantId,
            KnowledgeAssetId = request.KnowledgeAssetId,
            ReviewerId = request.ReviewerId,
            NominatedByUserId = _currentUser.UserId,
            NominatedAt = DateTime.UtcNow,
            Status = AssetReviewStatus.Pending,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.KnowledgeAssetReviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            request.ReviewerId, _currentUser.TenantId,
            NotificationType.General,
            "Peer Review Requested",
            $"You have been nominated to review the knowledge asset \"{asset.Title}\".",
            "KnowledgeAssetReview", review.Id, cancellationToken);

        return await MapToDtoAsync(review, asset.Title, asset.AssetType.ToString(), cancellationToken);
    }

    public async Task<PagedResult<AssetReviewDto>> GetPendingReviewsAsync(
        GetPendingReviewsRequest request, CancellationToken cancellationToken)
    {
        var query = _db.KnowledgeAssetReviews
            .Where(r => r.TenantId == _currentUser.TenantId
                && r.ReviewerId == _currentUser.UserId
                && r.Status == AssetReviewStatus.Pending)
            .AsQueryable();

        var total = await query.CountAsync(cancellationToken);

        var reviews = await query
            .OrderBy(r => r.NominatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .Join(_db.KnowledgeAssets, r => r.KnowledgeAssetId, a => a.Id,
                (r, a) => new { Review = r, a.Title, AssetType = a.AssetType.ToString() })
            .ToListAsync(cancellationToken);

        var reviewerNames = await GetUserNamesAsync(
            reviews.Select(x => x.Review.ReviewerId).Distinct().ToList(), cancellationToken);
        var nominatorNames = await GetUserNamesAsync(
            reviews.Select(x => x.Review.NominatedByUserId).Distinct().ToList(), cancellationToken);

        var dtos = reviews.Select(x => new AssetReviewDto(
            x.Review.Id,
            x.Review.KnowledgeAssetId,
            x.Title,
            x.AssetType,
            x.Review.ReviewerId,
            reviewerNames.GetValueOrDefault(x.Review.ReviewerId, "Unknown"),
            nominatorNames.GetValueOrDefault(x.Review.NominatedByUserId, "Unknown"),
            x.Review.NominatedAt,
            x.Review.Status,
            x.Review.Comments,
            x.Review.ReviewedAt)).ToList();

        return new PagedResult<AssetReviewDto> { Data = dtos, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<AssetReviewDto> SubmitReviewAsync(
        Guid reviewId, SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        var review = await _db.KnowledgeAssetReviews
            .Include(r => r.KnowledgeAsset)
            .FirstOrDefaultAsync(r => r.Id == reviewId
                && r.TenantId == _currentUser.TenantId
                && r.ReviewerId == _currentUser.UserId,
                cancellationToken);

        if (review is null) throw new NotFoundException("KnowledgeAssetReview", reviewId);
        if (review.Status != AssetReviewStatus.Pending)
            throw new BusinessRuleException("This review has already been submitted.");
        if (review.KnowledgeAsset is null) throw new NotFoundException("KnowledgeAsset", review.KnowledgeAssetId);

        review.Status = request.Decision;
        review.Comments = request.Comments;
        review.ReviewedAt = DateTime.UtcNow;
        review.ModifiedBy = _currentUser.UserId;
        review.ModifiedOn = DateTime.UtcNow;
        review.RecordVersion++;

        if (request.Decision == AssetReviewStatus.Approved)
        {
            review.KnowledgeAsset.IsVerified = true;
            review.KnowledgeAsset.VerifiedById = _currentUser.UserId;
            review.KnowledgeAsset.VerifiedAt = DateTime.UtcNow;
            review.KnowledgeAsset.ModifiedBy = _currentUser.UserId;
            review.KnowledgeAsset.ModifiedOn = DateTime.UtcNow;

            await _notificationService.SendAsync(
                review.KnowledgeAsset.CreatedBy, _currentUser.TenantId,
                NotificationType.General,
                "Asset Verified",
                $"Your knowledge asset \"{review.KnowledgeAsset.Title}\" has been verified by a peer reviewer.",
                "KnowledgeAsset", review.KnowledgeAssetId, cancellationToken);
        }
        else if (request.Decision == AssetReviewStatus.Rejected)
        {
            await _notificationService.SendAsync(
                review.KnowledgeAsset.CreatedBy, _currentUser.TenantId,
                NotificationType.General,
                "Asset Review Result",
                $"A peer review decision has been made on \"{review.KnowledgeAsset.Title}\". Decision: {request.Decision}. Comments: {request.Comments}",
                "KnowledgeAsset", review.KnowledgeAssetId, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var assetTitle = review.KnowledgeAsset.Title;
        var assetType = review.KnowledgeAsset.AssetType.ToString();

        return await MapToDtoAsync(review, assetTitle, assetType, cancellationToken);
    }

    public async Task<List<AssetReviewDto>> GetAssetReviewsAsync(
        Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await _db.KnowledgeAssets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.TenantId == _currentUser.TenantId, cancellationToken);

        if (asset is null) throw new NotFoundException("KnowledgeAsset", assetId);

        var reviews = await _db.KnowledgeAssetReviews
            .Where(r => r.KnowledgeAssetId == assetId && r.TenantId == _currentUser.TenantId)
            .OrderByDescending(r => r.NominatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var allUserIds = reviews.SelectMany(r => new[] { r.ReviewerId, r.NominatedByUserId }).Distinct().ToList();
        var names = await GetUserNamesAsync(allUserIds, cancellationToken);

        return reviews.Select(r => new AssetReviewDto(
            r.Id, r.KnowledgeAssetId, asset.Title, asset.AssetType.ToString(),
            r.ReviewerId,
            names.GetValueOrDefault(r.ReviewerId, "Unknown"),
            names.GetValueOrDefault(r.NominatedByUserId, "Unknown"),
            r.NominatedAt, r.Status, r.Comments, r.ReviewedAt)).ToList();
    }

    private async Task<AssetReviewDto> MapToDtoAsync(
        KnowledgeAssetReview review, string assetTitle, string assetType,
        CancellationToken cancellationToken)
    {
        var reviewerName = await _db.Users
            .Where(u => u.Id == review.ReviewerId)
            .AsNoTracking()
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Unknown";

        var nominatorName = await _db.Users
            .Where(u => u.Id == review.NominatedByUserId)
            .AsNoTracking()
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Unknown";

        return new AssetReviewDto(
            review.Id, review.KnowledgeAssetId, assetTitle, assetType,
            review.ReviewerId, reviewerName, nominatorName,
            review.NominatedAt, review.Status, review.Comments, review.ReviewedAt);
    }

    private async Task<Dictionary<Guid, string>> GetUserNamesAsync(
        List<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, string>();

        return await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
    }
}
