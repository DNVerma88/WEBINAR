namespace KnowHub.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid? SessionId { get; set; }
    public Guid? KnowledgeAssetId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public bool IsDeleted { get; set; }

    public Session? Session { get; set; }
    public KnowledgeAsset? KnowledgeAsset { get; set; }
    public User Author { get; set; } = null!;
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}
