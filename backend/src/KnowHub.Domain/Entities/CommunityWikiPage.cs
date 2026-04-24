namespace KnowHub.Domain.Entities;

public class CommunityWikiPage : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public Guid? ParentPageId { get; set; }
    public int OrderSequence { get; set; }
    public bool IsPublished { get; set; }
    public int ViewCount { get; set; }

    public Community? Community { get; set; }
    public User? Author { get; set; }
    public CommunityWikiPage? ParentPage { get; set; }
    public ICollection<CommunityWikiPage> ChildPages { get; set; } = new List<CommunityWikiPage>();
}
