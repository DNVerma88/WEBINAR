using FluentValidation;
using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Application.Utilities;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Markdig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHub.Infrastructure.Services;

public class CommunityPostService : ICommunityPostService
{
    private static readonly MarkdownPipeline _mdPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IValidator<CreatePostRequest> _createValidator;
    private readonly IValidator<UpdatePostRequest> _updateValidator;

    public CommunityPostService(
        KnowHubDbContext db,
        ICurrentUserAccessor currentUser,
        IServiceScopeFactory scopeFactory,
        IValidator<CreatePostRequest> createValidator,
        IValidator<UpdatePostRequest> updateValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _scopeFactory = scopeFactory;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private static void ThrowIfInvalid(FluentValidation.Results.ValidationResult result)
    {
        if (result.IsValid) return;
        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        throw new KnowHub.Domain.Exceptions.ValidationException(errors);
    }

    public async Task<PagedResult<CommunityPostSummaryDto>> GetPostsAsync(Guid communityId, GetPostsRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var tenantId = _currentUser.TenantId;

        // Expose both Published and Pinned posts in the feed
        var query = _db.CommunityPosts
            .Where(p => p.CommunityId == communityId && p.TenantId == tenantId
                && (p.Status == PostStatus.Published || p.Status == PostStatus.Pinned))
            .AsNoTracking();

        if (request.PostType.HasValue)
            query = query.Where(p => p.PostType == request.PostType.Value);

        if (!string.IsNullOrWhiteSpace(request.TagSlug))
            query = query.Where(p => p.Tags.Any(t => t.Tag.Slug == request.TagSlug));

        query = request.SortBy switch
        {
            PostSortBy.Trending => query.OrderByDescending(p => p.ViewCount).ThenByDescending(p => p.ReactionCount),
            PostSortBy.Top => query.OrderByDescending(p => p.ReactionCount).ThenByDescending(p => p.CommentCount),
            _ => query.OrderByDescending(p => p.Status == PostStatus.Pinned).ThenByDescending(p => p.IsFeatured).ThenByDescending(p => p.PublishedAt)
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
            .ToPagedListAsync(request.PageNumber, request.PageSize, ct);

        return new PagedResult<CommunityPostSummaryDto> { Data = data, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<CommunityPostDetailDto> GetPostAsync(Guid communityId, Guid postId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var tenantId = _currentUser.TenantId;
        var isAdmin = _currentUser.IsAdminOrAbove;

        // OWASP A01: Restrict draft/scheduled posts to author or admin only
        var baseQuery = _db.CommunityPosts
            .Where(p => p.Id == postId && p.CommunityId == communityId && p.TenantId == tenantId);

        if (!isAdmin)
            baseQuery = baseQuery.Where(p =>
                p.Status == PostStatus.Published ||
                p.Status == PostStatus.Pinned ||
                p.AuthorId == userId);

        var post = await baseQuery
            .AsNoTracking()
            .Select(p => new CommunityPostDetailDto
            {
                Id = p.Id,
                CommunityId = p.CommunityId,
                AuthorId = p.AuthorId,
                AuthorName = p.Author.FullName,
                AuthorAvatarUrl = p.Author.ProfilePhotoUrl,
                Title = p.Title,
                Slug = p.Slug,
                CoverImageUrl = p.CoverImageUrl,
                ContentHtml = p.ContentHtml,
                ContentMarkdown = p.ContentMarkdown,
                CanonicalUrl = p.CanonicalUrl,
                PostType = p.PostType,
                Status = p.Status,
                ReadingTimeMinutes = p.ReadingTimeMinutes,
                ReactionCount = p.ReactionCount,
                CommentCount = p.CommentCount,
                ViewCount = p.ViewCount,
                BookmarkCount = p.BookmarkCount,
                PublishedAt = p.PublishedAt,
                IsFeatured = p.IsFeatured,
                SeriesId = p.SeriesId,
                SeriesTitle = p.Series != null ? p.Series.Title : null,
                HasBookmarked = p.Bookmarks.Any(b => b.UserId == userId),
                UserReactions = p.Reactions.Where(r => r.UserId == userId).Select(r => r.ReactionType).ToList(),
                Tags = p.Tags.Select(t => new CommunityPostTagDto
                {
                    TagId = t.TagId,
                    Name = t.Tag.Name,
                    Slug = t.Tag.Slug
                }).ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Post", postId);

        // Increment view count fire-and-forget using a fresh scope (DbContext is not thread-safe)
        var capturedPostId = postId;
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<KnowHubDbContext>();
                await db.CommunityPosts
                    .Where(p => p.Id == capturedPostId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));
            }
            catch { /* best-effort */ }
        });

        return post;
    }

    public async Task<CommunityPostDetailDto> CreatePostAsync(Guid communityId, CreatePostRequest request, CancellationToken ct)
    {
        ThrowIfInvalid(await _createValidator.ValidateAsync(request, ct));

        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        var community = await _db.Communities
            .Where(c => c.Id == communityId && c.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Community", communityId);

        // OWASP A01: Only community members (or admins) may post
        if (!_currentUser.IsAdminOrAbove)
        {
            var isMember = await _db.CommunityMembers
                .AnyAsync(m => m.CommunityId == communityId && m.UserId == userId && m.TenantId == tenantId, ct);
            if (!isMember)
                throw new ForbiddenException("You must be a community member to create posts.");
        }

        var sanitized = MarkdownSanitizer.Sanitize(request.ContentMarkdown);
        var contentHtml = Markdown.ToHtml(sanitized, _mdPipeline);
        var wordCount = sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var readingMinutes = Math.Max(1, wordCount / 200);

        var slug = GenerateSlug(request.Title);
        var slugExists = await _db.CommunityPosts.AnyAsync(p => p.CommunityId == communityId && p.Slug == slug && p.TenantId == tenantId, ct);
        if (slugExists) slug = $"{slug}-{Guid.NewGuid():N}"[..300];

        var post = new CommunityPost
        {
            TenantId = tenantId,
            CommunityId = communityId,
            AuthorId = userId,
            Title = request.Title,
            Slug = slug,
            ContentMarkdown = sanitized,
            ContentHtml = contentHtml,
            CoverImageUrl = request.CoverImageUrl,
            CanonicalUrl = request.CanonicalUrl,
            PostType = request.PostType,
            ReadingTimeMinutes = readingMinutes,
            Status = request.PublishImmediately ? PostStatus.Published : (request.ScheduledAt.HasValue ? PostStatus.Scheduled : PostStatus.Draft),
            PublishedAt = request.PublishImmediately ? DateTime.UtcNow : null,
            ScheduledAt = request.ScheduledAt,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        if (request.TagSlugs.Count > 0)
        {
            var tags = await _db.Tags
                .Where(t => request.TagSlugs.Contains(t.Slug) && t.TenantId == tenantId)
                .ToListAsync(ct);
            foreach (var tag in tags)
                post.Tags.Add(new CommunityPostTag { PostId = post.Id, TagId = tag.Id, TenantId = tenantId });
        }

        _db.CommunityPosts.Add(post);
        await _db.SaveChangesAsync(ct);

        return await GetPostAsync(communityId, post.Id, ct);
    }

    public async Task<CommunityPostDetailDto> UpdatePostAsync(Guid communityId, Guid postId, UpdatePostRequest request, CancellationToken ct)
    {
        ThrowIfInvalid(await _updateValidator.ValidateAsync(request, ct));

        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        var post = await _db.CommunityPosts
            .Include(p => p.Tags)
            .Where(p => p.Id == postId && p.CommunityId == communityId && p.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Post", postId);

        if (post.AuthorId != userId && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("You can only edit your own posts.");

        var sanitized = MarkdownSanitizer.Sanitize(request.ContentMarkdown);
        var wordCount = sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        post.Title = request.Title;
        post.ContentMarkdown = sanitized;
        post.ContentHtml = Markdown.ToHtml(sanitized, _mdPipeline);
        post.CoverImageUrl = request.CoverImageUrl;
        post.CanonicalUrl = request.CanonicalUrl;
        post.ReadingTimeMinutes = Math.Max(1, wordCount / 200);
        post.ModifiedBy = userId;
        post.ModifiedOn = DateTime.UtcNow;
        post.RecordVersion++;

        if (request.Status.HasValue && post.Status == PostStatus.Draft && request.Status == PostStatus.Published)
        {
            post.Status = PostStatus.Published;
            post.PublishedAt = DateTime.UtcNow;
        }

        // Replace tags
        post.Tags.Clear();
        if (request.TagSlugs.Count > 0)
        {
            var tags = await _db.Tags
                .Where(t => request.TagSlugs.Contains(t.Slug) && t.TenantId == tenantId)
                .ToListAsync(ct);
            foreach (var tag in tags)
                post.Tags.Add(new CommunityPostTag { PostId = post.Id, TagId = tag.Id, TenantId = tenantId });
        }

        await _db.SaveChangesAsync(ct);
        return await GetPostAsync(communityId, post.Id, ct);
    }

    public async Task DeletePostAsync(Guid communityId, Guid postId, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        var post = await _db.CommunityPosts
            .Where(p => p.Id == postId && p.CommunityId == communityId && p.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Post", postId);

        if (post.AuthorId != userId && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("You can only delete your own posts.");

        _db.CommunityPosts.Remove(post);
        await _db.SaveChangesAsync(ct);
    }

    public async Task TogglePinAsync(Guid communityId, Guid postId, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only admins can pin posts.");

        var tenantId = _currentUser.TenantId;
        var post = await _db.CommunityPosts
            .Where(p => p.Id == postId && p.CommunityId == communityId && p.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Post", postId);

        post.Status = post.Status == PostStatus.Pinned ? PostStatus.Published : PostStatus.Pinned;
        post.ModifiedOn = DateTime.UtcNow;
        post.ModifiedBy = _currentUser.UserId;
        post.RecordVersion++;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveDraftAsync(Guid communityId, Guid postId, DraftPostRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        var post = await _db.CommunityPosts
            .Where(p => p.Id == postId && p.CommunityId == communityId && p.TenantId == tenantId && p.AuthorId == userId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Post", postId);

        if (post.Status != PostStatus.Draft)
            throw new ForbiddenException("Only drafts can be auto-saved.");

        if (request.Title is not null) post.Title = request.Title;
        if (request.ContentMarkdown is not null)
        {
            var sanitized = MarkdownSanitizer.Sanitize(request.ContentMarkdown);
            post.ContentMarkdown = sanitized;
            post.ContentHtml = Markdown.ToHtml(sanitized, _mdPipeline);
        }
        post.LastDraftSavedAt = DateTime.UtcNow;
        post.ModifiedOn = DateTime.UtcNow;
        post.ModifiedBy = userId;

        await _db.SaveChangesAsync(ct);
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace(":", "")
            .Replace(";", "")
            .Replace("&", "and")
            .Replace("/", "-")
            .Replace("\\", "-");
        return slug.Length > 290 ? slug[..290] : slug;
    }

    // Phase 5 — Full-text search using the tsvector GIN index
    public async Task<PagedResult<CommunityPostSummaryDto>> SearchPostsAsync(Guid communityId, string query, int pageNumber, int pageSize, CancellationToken ct)
    {
        pageSize   = Math.Clamp(pageSize, 1, 100);
        pageNumber = Math.Max(1, pageNumber);

        if (string.IsNullOrWhiteSpace(query))
            return new PagedResult<CommunityPostSummaryDto> { Data = Array.Empty<CommunityPostSummaryDto>(), TotalCount = 0, PageNumber = pageNumber, PageSize = pageSize };

        var tenantId   = _currentUser.TenantId;
        var userId     = _currentUser.UserId;

        // Sanitise query: keep only alphanumeric + spaces, replace spaces with & for tsquery
        var sanitised = System.Text.RegularExpressions.Regex
            .Replace(query.Trim(), @"[^a-zA-Z0-9\s]", "")
            .Trim();

        if (string.IsNullOrWhiteSpace(sanitised))
            return new PagedResult<CommunityPostSummaryDto> { Data = Array.Empty<CommunityPostSummaryDto>(), TotalCount = 0, PageNumber = pageNumber, PageSize = pageSize };

        var formattedQuery = string.Join(" & ", sanitised.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        // Use raw Npgsql full-text search
        var baseQuery = _db.CommunityPosts
            .Where(p => p.TenantId == tenantId
                        && p.CommunityId == communityId
                        && p.Status == PostStatus.Published
                        && EF.Functions.ToTsVector("english", p.Title + " " + p.ContentMarkdown)
                            .Matches(EF.Functions.ToTsQuery("english", formattedQuery)));

        var bookmarkedIds = await _db.PostBookmarks
            .Where(b => b.UserId == userId && b.TenantId == tenantId)
            .Select(b => b.PostId)
            .ToListAsync(ct);

        var total = await baseQuery.CountAsync(ct);
        var posts = await baseQuery
            .OrderByDescending(p => p.PublishedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Author)
            .Include(p => p.Tags).ThenInclude(t => t.Tag)
            .AsNoTracking()
            .Select(p => new CommunityPostSummaryDto
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
                }).ToList()
            })
            .ToListAsync(ct);

        return new PagedResult<CommunityPostSummaryDto> { Data = posts, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }
}
