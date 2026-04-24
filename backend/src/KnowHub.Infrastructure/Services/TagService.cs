using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class TagService : ITagService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public TagService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<TagDto>> GetTagsAsync(GetTagsRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Tags
            .Where(t => t.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(t => t.Name.Contains(request.SearchTerm));

        if (request.IsActive.HasValue)
            query = query.Where(t => t.IsActive == request.IsActive.Value);

        var (data, total) = await query
            .OrderBy(t => t.Name)
            .Select(t => new TagDto { Id = t.Id, Name = t.Name, Slug = t.Slug, Description = t.Description, UsageCount = t.UsageCount, PostCount = t.PostCount, IsActive = t.IsActive, IsOfficial = t.IsOfficial })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResult<TagDto> { Data = data, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<TagDto> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove && !_currentUser.IsInRole(UserRole.KnowledgeTeam))
            throw new ForbiddenException("Only Admin or KnowledgeTeam members can create tags.");

        var slug = request.Name.ToLowerInvariant().Replace(' ', '-');
        var exists = await _db.Tags.AnyAsync(t => t.Slug == slug && t.TenantId == _currentUser.TenantId, cancellationToken);
        if (exists) throw new ConflictException($"A tag with name '{request.Name}' already exists.");

        var tag = new Domain.Entities.Tag
        {
            TenantId = _currentUser.TenantId,
            Name = request.Name,
            Slug = slug,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync(cancellationToken);
        return new TagDto { Id = tag.Id, Name = tag.Name, Slug = tag.Slug, UsageCount = 0, IsActive = true };
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == _currentUser.TenantId, cancellationToken);
        if (tag is null) throw new NotFoundException("Tag", id);

        tag.IsActive = false;
        tag.ModifiedOn = DateTime.UtcNow;
        tag.ModifiedBy = _currentUser.UserId;
        tag.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<CommunityPostSummaryDto>> GetPostsByTagAsync(string tagSlug, GetPostsRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        var tag = await _db.Tags
            .Where(t => t.Slug == tagSlug && t.TenantId == tenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Tag '{tagSlug}' not found.");

        var query = _db.CommunityPosts
            .Where(p => p.TenantId == tenantId
                && p.Status == Domain.Enums.PostStatus.Published
                && p.Tags.Any(t => t.TagId == tag.Id))
            .AsNoTracking();

        query = request.SortBy switch
        {
            PostSortBy.Trending => query.OrderByDescending(p => p.ViewCount).ThenByDescending(p => p.ReactionCount),
            PostSortBy.Top => query.OrderByDescending(p => p.ReactionCount).ThenByDescending(p => p.CommentCount),
            _ => query.OrderByDescending(p => p.PublishedAt)
        };

        var (data, total) = await query
            .Select(p => new CommunityPostSummaryDto
            {
                Id = p.Id,
                CommunityId = p.CommunityId,
                AuthorId = p.AuthorId,
                AuthorName = p.Author.FullName,
                AuthorAvatarUrl = p.Author.ProfilePhotoUrl,
                Title = p.Title,
                Slug = p.Slug,
                CoverImageUrl = p.CoverImageUrl,
                PostType = p.PostType,
                Status = p.Status,
                ReadingTimeMinutes = p.ReadingTimeMinutes,
                ReactionCount = p.ReactionCount,
                CommentCount = p.CommentCount,
                ViewCount = p.ViewCount,
                BookmarkCount = p.BookmarkCount,
                PublishedAt = p.PublishedAt,
                IsFeatured = p.IsFeatured,
                HasBookmarked = p.Bookmarks.Any(b => b.UserId == userId),
                Tags = p.Tags.Select(t => new CommunityPostTagDto
                {
                    TagId = t.TagId,
                    Name = t.Tag.Name,
                    Slug = t.Tag.Slug
                }).ToList()
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResult<CommunityPostSummaryDto> { Data = data, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<bool> ToggleFollowTagAsync(string tagSlug, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        var tag = await _db.Tags
            .Where(t => t.Slug == tagSlug && t.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Tag '{tagSlug}' not found.");

        var existing = await _db.UserTagFollows
            .Where(f => f.FollowerId == userId && f.FollowedTagId == tag.Id && f.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            _db.UserTagFollows.Add(new Domain.Entities.UserTagFollow
            {
                TenantId = tenantId,
                FollowerId = userId,
                FollowedTagId = tag.Id,
                FollowedAt = DateTime.UtcNow,
                CreatedBy = userId,
                ModifiedBy = userId
            });
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        _db.UserTagFollows.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken);
        return false;
    }
}
