namespace KnowHub.Domain.Entities;

public class Like : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? KnowledgeAssetId { get; set; }
    public Guid? CommentId { get; set; }
    public Guid? KnowledgeRequestId { get; set; }

    public User User { get; set; } = null!;
    public Session? Session { get; set; }
    public KnowledgeAsset? KnowledgeAsset { get; set; }
    public Comment? Comment { get; set; }
    public KnowledgeRequest? KnowledgeRequest { get; set; }
}
