namespace KnowHub.Application.Contracts;

public class WikiPageDto
{
    public Guid Id { get; init; }
    public Guid CommunityId { get; init; }
    public Guid AuthorId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string ContentMarkdown { get; init; } = string.Empty;
    public Guid? ParentPageId { get; init; }
    public int OrderSequence { get; init; }
    public bool IsPublished { get; init; }
    public int ViewCount { get; init; }
    public DateTime CreatedDate { get; init; }
}

public class CreateWikiPageRequest
{
    public string Title { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public Guid? ParentPageId { get; set; }
    public int OrderSequence { get; set; }
    public bool IsPublished { get; set; }
}

public class UpdateWikiPageRequest
{
    public string Title { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public Guid? ParentPageId { get; set; }
    public int OrderSequence { get; set; }
    public bool IsPublished { get; set; }
}
