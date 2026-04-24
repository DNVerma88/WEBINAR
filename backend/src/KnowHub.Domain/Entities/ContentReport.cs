using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class ContentReport : BaseEntity
{
    public Guid ReporterId { get; set; }
    public Guid? TargetPostId { get; set; }
    public Guid? TargetCommentId { get; set; }
    public ReportReason ReasonCode { get; set; }
    public string? Description { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public Guid? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Navigation
    public User? Reporter { get; set; }
    public CommunityPost? TargetPost { get; set; }
    public PostComment? TargetComment { get; set; }
    public User? Resolver { get; set; }
}
