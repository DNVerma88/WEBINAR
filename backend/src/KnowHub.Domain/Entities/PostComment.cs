namespace KnowHub.Domain.Entities;

public class PostComment : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string BodyMarkdown { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public int LikeCount { get; set; }

    public CommunityPost? Post { get; set; }
    public User? Author { get; set; }
    public PostComment? ParentComment { get; set; }
    public ICollection<PostComment> Replies { get; set; } = new List<PostComment>();
}
