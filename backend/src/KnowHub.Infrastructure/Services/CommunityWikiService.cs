using KnowHub.Application.Contracts;
using KnowHub.Application.Utilities;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class CommunityWikiService : ICommunityWikiService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public CommunityWikiService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<WikiPageDto>> GetPagesAsync(Guid communityId, CancellationToken cancellationToken)
    {
        var query = _db.CommunityWikiPages
            .Include(p => p.Author)
            .Where(p => p.CommunityId == communityId && p.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (!_currentUser.IsAdminOrAbove) query = query.Where(p => p.IsPublished);

        return await query
            .OrderBy(p => p.OrderSequence)
            .Select(p => new WikiPageDto
            {
                Id = p.Id, CommunityId = p.CommunityId,
                AuthorId = p.AuthorId,
                AuthorName = p.Author != null ? p.Author.FullName : string.Empty,
                Title = p.Title, Slug = p.Slug,
                ContentMarkdown = p.ContentMarkdown,
                ParentPageId = p.ParentPageId,
                OrderSequence = p.OrderSequence,
                IsPublished = p.IsPublished, ViewCount = p.ViewCount,
                CreatedDate = p.CreatedDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<WikiPageDto> GetPageAsync(Guid communityId, Guid pageId, CancellationToken cancellationToken)
    {
        var page = await _db.CommunityWikiPages
            .Include(p => p.Author)
            .Where(p => p.Id == pageId && p.CommunityId == communityId && p.TenantId == _currentUser.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (page is null) throw new NotFoundException("WikiPage", pageId);
        if (!page.IsPublished && !_currentUser.IsAdminOrAbove && page.AuthorId != _currentUser.UserId)
            throw new ForbiddenException("This wiki page is not published.");

        page.ViewCount++;
        page.ModifiedOn = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(page);
    }

    public async Task<WikiPageDto> CreatePageAsync(
        Guid communityId, CreateWikiPageRequest request, CancellationToken cancellationToken)
    {
        var isMember = await _db.CommunityMembers
            .AnyAsync(m => m.CommunityId == communityId
                && m.UserId == _currentUser.UserId
                && m.TenantId == _currentUser.TenantId, cancellationToken);

        if (!isMember && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("You must be a community member to create wiki pages.");

        var sanitised = MarkdownSanitizer.Sanitize(request.ContentMarkdown);
        var slug = GenerateSlug(request.Title);
        await EnsureSlugUniqueAsync(communityId, slug, null, cancellationToken);

        var page = new CommunityWikiPage
        {
            TenantId = _currentUser.TenantId,
            CommunityId = communityId,
            AuthorId = _currentUser.UserId,
            Title = request.Title,
            Slug = slug,
            ContentMarkdown = sanitised,
            ParentPageId = request.ParentPageId,
            OrderSequence = request.OrderSequence,
            IsPublished = request.IsPublished,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.CommunityWikiPages.Add(page);
        await _db.SaveChangesAsync(cancellationToken);
        return MapToDto(page);
    }

    public async Task<WikiPageDto> UpdatePageAsync(
        Guid communityId, Guid pageId, UpdateWikiPageRequest request, CancellationToken cancellationToken)
    {
        var page = await _db.CommunityWikiPages
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == pageId
                && p.CommunityId == communityId
                && p.TenantId == _currentUser.TenantId, cancellationToken);

        if (page is null) throw new NotFoundException("WikiPage", pageId);

        var isModerator = await _db.CommunityMembers.AnyAsync(
            m => m.CommunityId == communityId && m.UserId == _currentUser.UserId
            && m.TenantId == _currentUser.TenantId
            && (m.MemberRole == CommunityMemberRole.Moderator
                || m.MemberRole == CommunityMemberRole.CoLeader), cancellationToken);

        var canEdit = _currentUser.IsAdminOrAbove
            || page.AuthorId == _currentUser.UserId
            || isModerator;

        if (!canEdit)
            throw new ForbiddenException("Only the page author, moderators, or Admins may update wiki pages.");

        page.Title = request.Title;
        page.ContentMarkdown = MarkdownSanitizer.Sanitize(request.ContentMarkdown);
        page.ParentPageId = request.ParentPageId;
        page.OrderSequence = request.OrderSequence;
        page.IsPublished = request.IsPublished;
        page.ModifiedBy = _currentUser.UserId;
        page.ModifiedOn = DateTime.UtcNow;
        page.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);
        return MapToDto(page);
    }

    public async Task DeletePageAsync(Guid communityId, Guid pageId, CancellationToken cancellationToken)
    {
        var page = await _db.CommunityWikiPages
            .FirstOrDefaultAsync(p => p.Id == pageId
                && p.CommunityId == communityId
                && p.TenantId == _currentUser.TenantId, cancellationToken);

        if (page is null) throw new NotFoundException("WikiPage", pageId);

        var isModerator = await _db.CommunityMembers.AnyAsync(
            m => m.CommunityId == communityId && m.UserId == _currentUser.UserId
            && m.TenantId == _currentUser.TenantId
            && (m.MemberRole == CommunityMemberRole.Moderator
                || m.MemberRole == CommunityMemberRole.CoLeader), cancellationToken);

        var canDelete = _currentUser.IsAdminOrAbove
            || page.AuthorId == _currentUser.UserId
            || isModerator;

        if (!canDelete)
            throw new ForbiddenException("Only the page author, moderators, or Admins may delete wiki pages.");

        _db.CommunityWikiPages.Remove(page);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSlugUniqueAsync(
        Guid communityId, string slug, Guid? excludePageId, CancellationToken cancellationToken)
    {
        var query = _db.CommunityWikiPages
            .Where(p => p.TenantId == _currentUser.TenantId
                && p.CommunityId == communityId
                && p.Slug == slug);

        if (excludePageId.HasValue) query = query.Where(p => p.Id != excludePageId.Value);

        if (await query.AnyAsync(cancellationToken))
            throw new ConflictException($"A wiki page with slug '{slug}' already exists in this community.");
    }

    private static string GenerateSlug(string title)
        => title.ToLowerInvariant().Replace(" ", "-").Trim('-');

    private static WikiPageDto MapToDto(CommunityWikiPage p) => new()
    {
        Id = p.Id, CommunityId = p.CommunityId, AuthorId = p.AuthorId,
        AuthorName = p.Author?.FullName ?? string.Empty,
        Title = p.Title, Slug = p.Slug, ContentMarkdown = p.ContentMarkdown,
        ParentPageId = p.ParentPageId, OrderSequence = p.OrderSequence,
        IsPublished = p.IsPublished, ViewCount = p.ViewCount, CreatedDate = p.CreatedDate
    };
}
