using KnowHub.Application.Models;
using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public class CommunityPostTagDto
{
    public Guid TagId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}

public class CommunityPostSummaryDto
{
    public Guid Id { get; init; }
    public Guid CommunityId { get; init; }
    public Guid AuthorId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string? AuthorAvatarUrl { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? CoverImageUrl { get; init; }
    public PostType PostType { get; init; }
    public PostStatus Status { get; init; }
    public int ReadingTimeMinutes { get; init; }
    public int ReactionCount { get; init; }
    public int CommentCount { get; init; }
    public long ViewCount { get; init; }
    public int BookmarkCount { get; init; }
    public DateTime? PublishedAt { get; init; }
    public bool IsFeatured { get; init; }
    public bool HasBookmarked { get; init; }
    public List<CommunityPostTagDto> Tags { get; init; } = new();
}

public class CommunityPostDetailDto : CommunityPostSummaryDto
{
    public string ContentHtml { get; init; } = string.Empty;
    public string ContentMarkdown { get; init; } = string.Empty;
    public string? CanonicalUrl { get; init; }
    public Guid? SeriesId { get; init; }
    public string? SeriesTitle { get; init; }
    public List<ReactionType> UserReactions { get; init; } = new();
}

public class PostReactionResultDto
{
    public Guid PostId { get; init; }
    public Dictionary<ReactionType, int> ReactionCounts { get; init; } = new();
    public List<ReactionType> UserReactions { get; init; } = new();
}

public class PostCommentDto
{
    public Guid Id { get; init; }
    public Guid PostId { get; init; }
    public Guid AuthorId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string? AuthorAvatarUrl { get; init; }
    public Guid? ParentCommentId { get; init; }
    public string BodyMarkdown { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
    public int LikeCount { get; init; }
    public DateTime CreatedDate { get; init; }
    public List<PostCommentDto> Replies { get; init; } = new();
}

public class PostBookmarkToggleResult
{
    public Guid PostId { get; init; }
    public bool IsBookmarked { get; init; }
    public int BookmarkCount { get; init; }
}

// ─── Requests ────────────────────────────────────────────────────────────────

public class GetPostsRequest
{
    public string? TagSlug { get; set; }
    public PostType? PostType { get; set; }
    public PostStatus? Status { get; set; }
    public PostSortBy SortBy { get; set; } = PostSortBy.Latest;
    public int PageNumber { get; set; } = 1;

    // Cap at 100 to prevent DoS-style over-fetching (OWASP A05)
    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, 100);
    }
}

public enum PostSortBy { Latest = 0, Trending = 1, Top = 2 }

public class CreatePostRequest
{
    public string Title { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public PostType PostType { get; set; } = PostType.Article;
    public string? CoverImageUrl { get; set; }
    public string? CanonicalUrl { get; set; }
    public List<string> TagSlugs { get; set; } = new();
    public bool PublishImmediately { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class UpdatePostRequest
{
    public string Title { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? CanonicalUrl { get; set; }
    public List<string> TagSlugs { get; set; } = new();
    public PostStatus? Status { get; set; }
}

public class DraftPostRequest
{
    public string? Title { get; set; }
    public string? ContentMarkdown { get; set; }
}

public class ToggleReactionRequest
{
    public ReactionType ReactionType { get; set; }
}

public class AddCommentRequest
{
    public string BodyMarkdown { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}

// ─── Service Interface ───────────────────────────────────────────────────────

public interface ICommunityPostService
{
    Task<PagedResult<CommunityPostSummaryDto>> GetPostsAsync(Guid communityId, GetPostsRequest request, CancellationToken ct);
    Task<CommunityPostDetailDto> GetPostAsync(Guid communityId, Guid postId, CancellationToken ct);
    Task<CommunityPostDetailDto> CreatePostAsync(Guid communityId, CreatePostRequest request, CancellationToken ct);
    Task<CommunityPostDetailDto> UpdatePostAsync(Guid communityId, Guid postId, UpdatePostRequest request, CancellationToken ct);
    Task DeletePostAsync(Guid communityId, Guid postId, CancellationToken ct);
    Task TogglePinAsync(Guid communityId, Guid postId, CancellationToken ct);
    Task SaveDraftAsync(Guid communityId, Guid postId, DraftPostRequest request, CancellationToken ct);
    Task<PagedResult<CommunityPostSummaryDto>> SearchPostsAsync(Guid communityId, string query, int pageNumber, int pageSize, CancellationToken ct);
}

public interface IPostReactionService
{
    Task<PostReactionResultDto> ToggleReactionAsync(Guid postId, ReactionType type, CancellationToken ct);
    Task<PostReactionResultDto> GetReactionsAsync(Guid postId, CancellationToken ct);
}

public interface IPostCommentService
{
    Task<PagedResult<PostCommentDto>> GetCommentsAsync(Guid postId, int pageNumber, int pageSize, CancellationToken ct);
    Task<PostCommentDto> AddCommentAsync(Guid postId, AddCommentRequest request, CancellationToken ct);
    Task DeleteCommentAsync(Guid commentId, CancellationToken ct);
}

public interface IPostBookmarkService
{
    Task<PostBookmarkToggleResult> ToggleBookmarkAsync(Guid postId, CancellationToken ct);
    Task<PagedResult<CommunityPostSummaryDto>> GetBookmarksAsync(int pageNumber, int pageSize, CancellationToken ct);
}
