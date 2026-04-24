using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using KnowHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class PostBookmarkService : IPostBookmarkService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public PostBookmarkService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PostBookmarkToggleResult> ToggleBookmarkAsync(Guid postId, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        var post = await _db.CommunityPosts
            .Where(p => p.Id == postId && p.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Post", postId);

        var existing = await _db.PostBookmarks
            .Where(b => b.PostId == postId && b.UserId == userId && b.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

        bool isBookmarked;
        if (existing is null)
        {
            _db.PostBookmarks.Add(new PostBookmark
            {
                TenantId = tenantId,
                UserId = userId,
                PostId = postId,
                BookmarkedAt = DateTime.UtcNow,
                CreatedBy = userId,
                ModifiedBy = userId
            });
            post.BookmarkCount++;
            isBookmarked = true;
        }
        else
        {
            _db.PostBookmarks.Remove(existing);
            post.BookmarkCount = Math.Max(0, post.BookmarkCount - 1);
            isBookmarked = false;
        }

        post.ModifiedOn = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new PostBookmarkToggleResult
        {
            PostId = postId,
            IsBookmarked = isBookmarked,
            BookmarkCount = post.BookmarkCount
        };
    }

    public async Task<PagedResult<CommunityPostSummaryDto>> GetBookmarksAsync(int pageNumber, int pageSize, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        var (data, total) = await _db.PostBookmarks
            .Where(b => b.UserId == userId && b.TenantId == tenantId)
            .AsNoTracking()
            .OrderByDescending(b => b.BookmarkedAt)
            .Select(b => new CommunityPostSummaryDto
            {
                Id = b.Post.Id,
                CommunityId = b.Post.CommunityId,
                AuthorId = b.Post.AuthorId,
                AuthorName = b.Post.Author.FullName,
                AuthorAvatarUrl = b.Post.Author.ProfilePhotoUrl,
                Title = b.Post.Title,
                Slug = b.Post.Slug,
                CoverImageUrl = b.Post.CoverImageUrl,
                PostType = b.Post.PostType,
                Status = b.Post.Status,
                ReadingTimeMinutes = b.Post.ReadingTimeMinutes,
                ReactionCount = b.Post.ReactionCount,
                CommentCount = b.Post.CommentCount,
                ViewCount = b.Post.ViewCount,
                BookmarkCount = b.Post.BookmarkCount,
                PublishedAt = b.Post.PublishedAt,
                IsFeatured = b.Post.IsFeatured,
                HasBookmarked = true,
                Tags = b.Post.Tags.Select(t => new CommunityPostTagDto
                {
                    TagId = t.TagId,
                    Name = t.Tag.Name,
                    Slug = t.Tag.Slug
                }).ToList()
            })
            .ToPagedListAsync(pageNumber, pageSize, ct);

        return new PagedResult<CommunityPostSummaryDto> { Data = data, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }
}
