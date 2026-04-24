using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class PostSeriesService : IPostSeriesService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public PostSeriesService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<PostSeriesDto>> GetSeriesAsync(Guid communityId, int pageNumber, int pageSize, CancellationToken ct)
    {
        pageSize   = Math.Clamp(pageSize, 1, 100);
        pageNumber = Math.Max(1, pageNumber);

        var tenantId = _currentUser.TenantId;

        var query = _db.PostSeries
            .Where(s => s.TenantId == tenantId && s.CommunityId == communityId);

        var total = await query.CountAsync(ct);
        var series = await query
            .OrderByDescending(s => s.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.Author)
            .Include(s => s.Posts).ThenInclude(p => p.Tags).ThenInclude(t => t.Tag)
            .Include(s => s.Posts).ThenInclude(p => p.Author)
            .AsNoTracking()
            .Select(s => MapToDto(s))
            .ToListAsync(ct);

        return new PagedResult<PostSeriesDto> { Data = series, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<PostSeriesDto> GetSeriesByIdAsync(Guid communityId, Guid seriesId, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;

        var series = await _db.PostSeries
            .Where(s => s.Id == seriesId && s.CommunityId == communityId && s.TenantId == tenantId)
            .Include(s => s.Author)
            .Include(s => s.Posts).ThenInclude(p => p.Tags).ThenInclude(t => t.Tag)
            .Include(s => s.Posts).ThenInclude(p => p.Author)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("PostSeries", seriesId);

        return MapToDto(series);
    }

    public async Task<PostSeriesDto> CreateSeriesAsync(Guid communityId, CreateSeriesRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId   = _currentUser.UserId;

        var slug = GenerateSlug(request.Title);

        // Ensure slug is unique within the community
        var slugExists = await _db.PostSeries
            .AnyAsync(s => s.CommunityId == communityId && s.Slug == slug && s.TenantId == tenantId, ct);

        if (slugExists)
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";

        var series = new PostSeries
        {
            TenantId    = tenantId,
            CommunityId = communityId,
            AuthorId    = userId,
            Title       = request.Title,
            Slug        = slug,
            Description = request.Description,
            CreatedBy   = userId,
            ModifiedBy  = userId
        };

        _db.PostSeries.Add(series);
        await _db.SaveChangesAsync(ct);

        return await GetSeriesByIdAsync(communityId, series.Id, ct);
    }

    public async Task<PostSeriesDto> UpdateSeriesAsync(Guid communityId, Guid seriesId, UpdateSeriesRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId   = _currentUser.UserId;

        var series = await _db.PostSeries
            .Where(s => s.Id == seriesId && s.CommunityId == communityId && s.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("PostSeries", seriesId);

        // Only author or admin can update
        if (series.AuthorId != userId && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("You do not have permission to update this series.");

        series.Title       = request.Title;
        series.Description = request.Description;
        series.ModifiedBy  = userId;
        series.ModifiedOn  = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetSeriesByIdAsync(communityId, seriesId, ct);
    }

    public async Task DeleteSeriesAsync(Guid communityId, Guid seriesId, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId   = _currentUser.UserId;

        var series = await _db.PostSeries
            .Where(s => s.Id == seriesId && s.CommunityId == communityId && s.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("PostSeries", seriesId);

        if (series.AuthorId != userId && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("You do not have permission to delete this series.");

        // Detach posts from series
        await _db.CommunityPosts
            .Where(p => p.SeriesId == seriesId)
            .ExecuteUpdateAsync(p => p
                .SetProperty(x => x.SeriesId, (Guid?)null)
                .SetProperty(x => x.SeriesOrder, (int?)null),
                ct);

        _db.PostSeries.Remove(series);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddPostToSeriesAsync(Guid communityId, Guid seriesId, Guid postId, int order, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId   = _currentUser.UserId;

        var series = await _db.PostSeries
            .Where(s => s.Id == seriesId && s.CommunityId == communityId && s.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("PostSeries", seriesId);

        if (series.AuthorId != userId && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("You do not have permission to modify this series.");

        var post = await _db.CommunityPosts
            .Where(p => p.Id == postId && p.CommunityId == communityId && p.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Post", postId);

        // Capture whether the post is being newly added to this series (vs. re-ordered).
        // The DB trigger also maintains PostCount on SeriesId change, giving defence-in-depth.
        bool isNewToSeries = post.SeriesId != seriesId;

        post.SeriesId    = seriesId;
        post.SeriesOrder = order;
        post.ModifiedBy  = userId;
        post.ModifiedOn  = DateTime.UtcNow;

        if (isNewToSeries)
            series.PostCount++;

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemovePostFromSeriesAsync(Guid communityId, Guid seriesId, Guid postId, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId   = _currentUser.UserId;

        var series = await _db.PostSeries
            .Where(s => s.Id == seriesId && s.CommunityId == communityId && s.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("PostSeries", seriesId);

        if (series.AuthorId != userId && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("You do not have permission to modify this series.");

        var post = await _db.CommunityPosts
            .Where(p => p.Id == postId && p.SeriesId == seriesId && p.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Post", postId);

        post.SeriesId    = null;
        post.SeriesOrder = null;
        post.ModifiedBy  = userId;
        post.ModifiedOn  = DateTime.UtcNow;

        series.PostCount = Math.Max(0, series.PostCount - 1);

        await _db.SaveChangesAsync(ct);
    }

    private static PostSeriesDto MapToDto(PostSeries s) => new()
    {
        Id          = s.Id,
        CommunityId = s.CommunityId,
        AuthorId    = s.AuthorId,
        AuthorName  = s.Author?.FullName ?? string.Empty,
        Title       = s.Title,
        Slug        = s.Slug,
        Description = s.Description,
        PostCount   = s.PostCount,
        Posts = s.Posts
            .OrderBy(p => p.SeriesOrder)
            .Select(p => new CommunityPostSummaryDto
            {
                Id                 = p.Id,
                CommunityId        = p.CommunityId,
                AuthorId           = p.AuthorId,
                AuthorName         = p.Author?.FullName ?? string.Empty,
                AuthorAvatarUrl    = p.Author?.ProfilePhotoUrl,
                Title              = p.Title,
                Slug               = p.Slug,
                CoverImageUrl      = p.CoverImageUrl,
                PostType           = p.PostType,
                Status             = p.Status,
                ReadingTimeMinutes = p.ReadingTimeMinutes,
                ReactionCount      = p.ReactionCount,
                CommentCount       = p.CommentCount,
                ViewCount          = p.ViewCount,
                BookmarkCount      = p.BookmarkCount,
                PublishedAt        = p.PublishedAt,
                IsFeatured         = p.IsFeatured,
                Tags = p.Tags.Select(t => new CommunityPostTagDto
                {
                    TagId = t.TagId,
                    Name  = t.Tag?.Name ?? string.Empty,
                    Slug  = t.Tag?.Slug ?? string.Empty
                }).ToList()
            })
            .ToList()
    };

    private static string GenerateSlug(string title) =>
        System.Text.RegularExpressions.Regex
            .Replace(title.ToLowerInvariant().Trim(), @"[^a-z0-9\s-]", "")
            .Replace(' ', '-')
            .Trim('-');
}

