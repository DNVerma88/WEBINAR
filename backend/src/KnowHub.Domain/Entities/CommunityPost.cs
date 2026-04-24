using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class CommunityPost : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid AuthorId { get; set; }
    public Guid? SeriesId { get; set; }
    public int? SeriesOrder { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? CanonicalUrl { get; set; }

    public PostType PostType { get; set; } = PostType.Article;
    public PostStatus Status { get; set; } = PostStatus.Draft;

    public int ReadingTimeMinutes { get; set; } = 1;
    public int ReactionCount { get; set; }
    public int CommentCount { get; set; }
    public long ViewCount { get; set; }
    public int BookmarkCount { get; set; }

    public DateTime? PublishedAt { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? LastDraftSavedAt { get; set; }

    // Navigation
    public Community? Community { get; set; }
    public User? Author { get; set; }
    public PostSeries? Series { get; set; }
    public ICollection<CommunityPostTag> Tags { get; set; } = new List<CommunityPostTag>();
    public ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
    public ICollection<PostBookmark> Bookmarks { get; set; } = new List<PostBookmark>();
}
