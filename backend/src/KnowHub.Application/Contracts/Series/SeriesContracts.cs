using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public class PostSeriesDto
{
    public Guid Id { get; init; }
    public Guid CommunityId { get; init; }
    public Guid AuthorId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int PostCount { get; init; }
    public List<CommunityPostSummaryDto> Posts { get; init; } = new();
}

// ─── Requests ─────────────────────────────────────────────────────────────────

public class CreateSeriesRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateSeriesRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ─── Interface ────────────────────────────────────────────────────────────────

public interface IPostSeriesService
{
    Task<PagedResult<PostSeriesDto>> GetSeriesAsync(Guid communityId, int pageNumber, int pageSize, CancellationToken ct);
    Task<PostSeriesDto> GetSeriesByIdAsync(Guid communityId, Guid seriesId, CancellationToken ct);
    Task<PostSeriesDto> CreateSeriesAsync(Guid communityId, CreateSeriesRequest request, CancellationToken ct);
    Task<PostSeriesDto> UpdateSeriesAsync(Guid communityId, Guid seriesId, UpdateSeriesRequest request, CancellationToken ct);
    Task DeleteSeriesAsync(Guid communityId, Guid seriesId, CancellationToken ct);
    Task AddPostToSeriesAsync(Guid communityId, Guid seriesId, Guid postId, int order, CancellationToken ct);
    Task RemovePostFromSeriesAsync(Guid communityId, Guid seriesId, Guid postId, CancellationToken ct);
}
