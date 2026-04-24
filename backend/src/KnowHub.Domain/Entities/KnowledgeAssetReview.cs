using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class KnowledgeAssetReview : BaseEntity
{
    public Guid KnowledgeAssetId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid NominatedByUserId { get; set; }
    public DateTime NominatedAt { get; set; } = DateTime.UtcNow;
    public AssetReviewStatus Status { get; set; } = AssetReviewStatus.Pending;
    public string? Comments { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public KnowledgeAsset? KnowledgeAsset { get; set; }
    public User? Reviewer { get; set; }
    public User? NominatedBy { get; set; }
}
