using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts.PeerReview;

public interface IPeerReviewService
{
    Task<AssetReviewDto> NominateReviewerAsync(NominateReviewerRequest request, CancellationToken cancellationToken);
    Task<PagedResult<AssetReviewDto>> GetPendingReviewsAsync(GetPendingReviewsRequest request, CancellationToken cancellationToken);
    Task<AssetReviewDto> SubmitReviewAsync(Guid reviewId, SubmitReviewRequest request, CancellationToken cancellationToken);
    Task<List<AssetReviewDto>> GetAssetReviewsAsync(Guid assetId, CancellationToken cancellationToken);
}
