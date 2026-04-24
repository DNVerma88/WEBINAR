using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Application.Models;
using KnowHub.Domain.Enums;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class FeedService : IFeedService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IDistributedCommunityCache _cache;

    public FeedService(KnowHubDbContext db, ICurrentUserAccessor currentUser, IDistributedCommunityCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<PagedResult<FeedPostDto>> GetPersonalizedFeedAsync(FeedRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId   = _currentUser.UserId;
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        // API-27: 4 pre-queries are sequential by nature (DbContext is not thread-safe)
        // Communities, tag-follows, and author-follows change infrequently and are worth caching
        // (addressed separately via distributed cache); bookmarks are moved to EXISTS in projection
        var memberCommunityIds = await _db.CommunityMembers
            .Where(m => m.UserId == userId && m.TenantId == tenantId)
            .Select(m => m.CommunityId)
            .ToListAsync(ct);

        var followedTagIds = await _db.UserTagFollows
            .Where(f => f.FollowerId == userId && f.TenantId == tenantId && f.FollowedTagId.HasValue)
            .Select(f => f.FollowedTagId!.Value)
            .ToListAsync(ct);

        var followedAuthorIds = await _db.UserTagFollows
            .Where(f => f.FollowerId == userId && f.TenantId == tenantId && f.FollowedUserId.HasValue)
            .Select(f => f.FollowedUserId!.Value)
            .ToListAsync(ct);

        var query = _db.CommunityPosts
            .Where(p => p.TenantId == tenantId
                        && p.Status == PostStatus.Published
                        && (memberCommunityIds.Contains(p.CommunityId)
                            || p.Tags.Any(t => followedTagIds.Contains(t.TagId))
                            || followedAuthorIds.Contains(p.AuthorId)));

        if (request.AfterId is not null && Guid.TryParse(request.AfterId, out var afterId))
        {
            var anchor = await _db.CommunityPosts
                .Where(p => p.Id == afterId)
                .Select(p => p.PublishedAt)
                .FirstOrDefaultAsync(ct);
            if (anchor.HasValue)
                query = query.Where(p => p.PublishedAt < anchor.Value);
        }

        var total = await query.CountAsync(ct);
        var posts = await query
            .OrderByDescending(p => p.PublishedAt)
            .Take(pageSize)
            // API-10: .Include() before .Select() is silently ignored by EF Core when projecting
            // to a non-entity type. Remove the dead Include chains; EF Core generates JOINs
            // automatically from the navigation property accesses inside .Select().
            .AsNoTracking()
            .Select(p => new FeedPostDto
            {
                Id                 = p.Id,
                CommunityId        = p.CommunityId,
                AuthorId           = p.AuthorId,
                AuthorName         = p.Author!.FullName,
                AuthorAvatarUrl    = p.Author.ProfilePhotoUrl,
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
                // API-11: server-side EXISTS instead of client-side IN(guid, guid, ...) from bookmarkedIds list
                HasBookmarked      = _db.PostBookmarks.Any(b => b.PostId == p.Id && b.UserId == userId && b.TenantId == tenantId),
                Tags               = p.Tags.Select(t => new CommunityPostTagDto
                {
                    TagId = t.TagId,
                    Name  = t.Tag!.Name,
                    Slug  = t.Tag.Slug
                }).ToList(),
                CommunityName = p.Community!.Name,
                CommunitySlug = p.Community.Slug
            })
            .ToListAsync(ct);

        return new PagedResult<FeedPostDto> { Data = posts, TotalCount = total, PageNumber = 1, PageSize = pageSize };
    }

    public async Task<PagedResult<FeedPostDto>> GetLatestAsync(FeedRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId   = _currentUser.UserId;
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var query = _db.CommunityPosts
            .Where(p => p.TenantId == tenantId && p.Status == PostStatus.Published);

        if (request.AfterId is not null && Guid.TryParse(request.AfterId, out var afterId))
        {
            var anchor = await _db.CommunityPosts
                .Where(p => p.Id == afterId)
                .Select(p => p.PublishedAt)
                .FirstOrDefaultAsync(ct);
            if (anchor.HasValue)
                query = query.Where(p => p.PublishedAt < anchor.Value);
        }

        var total = await query.CountAsync(ct);
        var posts = await query
            .OrderByDescending(p => p.PublishedAt)
            .Take(pageSize)
            .AsNoTracking()
            .Select(p => new FeedPostDto
            {
                Id                 = p.Id,
                CommunityId        = p.CommunityId,
                AuthorId           = p.AuthorId,
                AuthorName         = p.Author!.FullName,
                AuthorAvatarUrl    = p.Author.ProfilePhotoUrl,
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
                HasBookmarked      = _db.PostBookmarks.Any(b => b.PostId == p.Id && b.UserId == userId && b.TenantId == tenantId),
                Tags               = p.Tags.Select(t => new CommunityPostTagDto
                {
                    TagId = t.TagId,
                    Name  = t.Tag!.Name,
                    Slug  = t.Tag.Slug
                }).ToList(),
                CommunityName = p.Community!.Name,
                CommunitySlug = p.Community.Slug
            })
            .ToListAsync(ct);

        return new PagedResult<FeedPostDto> { Data = posts, TotalCount = total, PageNumber = 1, PageSize = pageSize };
    }

    public async Task<PagedResult<FeedPostDto>> GetTrendingAsync(int pageNumber, int pageSize, CancellationToken ct)
    {
        pageSize   = Math.Clamp(pageSize, 1, 50);
        pageNumber = Math.Max(1, pageNumber);

        var tenantId = _currentUser.TenantId;
        var userId   = _currentUser.UserId;

        // Try Redis trending sorted set first
        var trendingIds = await _cache.GetTrendingPostIdsAsync(tenantId, 100, ct);

        IQueryable<CommunityPost> query;
        if (trendingIds.Count > 0)
        {
            query = _db.CommunityPosts
                .Where(p => p.TenantId == tenantId
                            && p.Status == PostStatus.Published
                            && trendingIds.Contains(p.Id));
        }
        else
        {
            // Fallback: last 7 days, weighted score
            var since = DateTime.UtcNow.AddDays(-7);
            query = _db.CommunityPosts
                .Where(p => p.TenantId == tenantId
                            && p.Status == PostStatus.Published
                            && p.PublishedAt >= since);
        }

        var total = await query.CountAsync(ct);
        var posts = await query
            .OrderByDescending(p => p.ReactionCount * 3 + p.CommentCount * 2 + p.BookmarkCount)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .Select(p => new FeedPostDto
            {
                Id                 = p.Id,
                CommunityId        = p.CommunityId,
                AuthorId           = p.AuthorId,
                AuthorName         = p.Author!.FullName,
                AuthorAvatarUrl    = p.Author.ProfilePhotoUrl,
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
                HasBookmarked      = _db.PostBookmarks.Any(b => b.PostId == p.Id && b.UserId == userId && b.TenantId == tenantId),
                Tags               = p.Tags.Select(t => new CommunityPostTagDto
                {
                    TagId = t.TagId,
                    Name  = t.Tag!.Name,
                    Slug  = t.Tag.Slug
                }).ToList(),
                CommunityName = p.Community!.Name,
                CommunitySlug = p.Community.Slug
            })
            .ToListAsync(ct);

        return new PagedResult<FeedPostDto> { Data = posts, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }
}
        }
        else
        {
            // Fallback: last 7 days, weighted score
            var since = DateTime.UtcNow.AddDays(-7);
            query = _db.CommunityPosts
                .Where(p => p.TenantId == tenantId
                            && p.Status == PostStatus.Published
                            && p.PublishedAt >= since);
        }

        var total = await query.CountAsync(ct);
        var posts = await query
            .OrderByDescending(p => p.ReactionCount * 3 + p.CommentCount * 2 + p.BookmarkCount)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Author)
            .Include(p => p.Community)
            .Include(p => p.Tags).ThenInclude(t => t.Tag)
            .AsNoTracking()
            .Select(p => new FeedPostDto
            {
                Id                 = p.Id,
                CommunityId        = p.CommunityId,
                AuthorId           = p.AuthorId,
                AuthorName         = p.Author!.FullName,
                AuthorAvatarUrl    = p.Author.ProfilePhotoUrl,
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
                HasBookmarked      = bookmarkedIds.Contains(p.Id),
                Tags               = p.Tags.Select(t => new CommunityPostTagDto
                {
                    TagId = t.TagId,
                    Name  = t.Tag!.Name,
                    Slug  = t.Tag.Slug
                }).ToList(),
                CommunityName = p.Community!.Name,
                CommunitySlug = p.Community.Slug
            })
            .ToListAsync(ct);

        return new PagedResult<FeedPostDto> { Data = posts, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }
}

