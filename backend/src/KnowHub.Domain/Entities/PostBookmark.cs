namespace KnowHub.Domain.Entities;

public class PostBookmark : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
    public DateTime BookmarkedAt { get; set; } = DateTime.UtcNow;

    public CommunityPost? Post { get; set; }
    public User? User { get; set; }
}
