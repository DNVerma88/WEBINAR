namespace KnowHub.Domain.Entities;

public class PostSeries : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PostCount { get; set; }

    public Community? Community { get; set; }
    public User? Author { get; set; }
    public ICollection<CommunityPost> Posts { get; set; } = new List<CommunityPost>();
}
