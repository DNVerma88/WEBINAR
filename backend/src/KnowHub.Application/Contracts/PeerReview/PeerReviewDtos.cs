using KnowHub.Application.Models;
using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts.PeerReview;

public record NominateReviewerRequest(
    Guid KnowledgeAssetId,
    Guid ReviewerId
);

public record SubmitReviewRequest(
    AssetReviewStatus Decision,
    string? Comments
);

public record GetPendingReviewsRequest(
    int PageNumber = 1,
    int PageSize = 20
);

public record AssetReviewDto(
    Guid Id,
    Guid KnowledgeAssetId,
    string AssetTitle,
    string AssetType,
    Guid ReviewerId,
    string ReviewerName,
    string NominatedByUserName,
    DateTime NominatedAt,
    AssetReviewStatus Status,
    string? Comments,
    DateTime? ReviewedAt
);
