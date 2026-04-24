using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public class UserSummaryDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public bool IsFollowedByCurrentUser { get; init; }
}

public class FeedPostDto : CommunityPostSummaryDto
{
    public string CommunityName { get; init; } = string.Empty;
    public string CommunitySlug { get; init; } = string.Empty;
}

public class TrendingPostDto : FeedPostDto
{
    public double TrendingScore { get; init; }
}

// ─── Requests ────────────────────────────────────────────────────────────────

public class FeedRequest
{
    public string? AfterId { get; set; }   // cursor-based pagination
    public DateTime? AfterDate { get; set; }
    public int PageSize { get; set; } = 20;
}

// ─── Interfaces ──────────────────────────────────────────────────────────────

public interface IFeedService
{
    Task<PagedResult<FeedPostDto>> GetPersonalizedFeedAsync(FeedRequest request, CancellationToken ct);
    Task<PagedResult<FeedPostDto>> GetLatestAsync(FeedRequest request, CancellationToken ct);
    Task<PagedResult<FeedPostDto>> GetTrendingAsync(int pageNumber, int pageSize, CancellationToken ct);
}

public interface IUserFollowService
{
    Task<bool> ToggleFollowUserAsync(Guid targetUserId, CancellationToken ct);
    Task<PagedResult<UserSummaryDto>> GetFollowersAsync(Guid userId, int pageNumber, int pageSize, CancellationToken ct);
    Task<PagedResult<UserSummaryDto>> GetFollowingAsync(Guid userId, int pageNumber, int pageSize, CancellationToken ct);
}
