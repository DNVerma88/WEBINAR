using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class CommunityService : ICommunityService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public CommunityService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<CommunityDto>> GetCommunitiesAsync(GetCommunitiesRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Communities
            .Where(c => c.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (request.IsActive.HasValue) query = query.Where(c => c.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(c => c.Name.Contains(request.SearchTerm) || (c.Description != null && c.Description.Contains(request.SearchTerm)));

        var userId = _currentUser.UserId;

        var (data, total) = await query
            .OrderBy(c => c.Name)
            .Select(c => new CommunityDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                IconName = c.IconName,
                CoverImageUrl = c.CoverImageUrl,
                MemberCount = c.MemberCount,
                IsActive = c.IsActive,
                IsMember = c.Members.Any(m => m.UserId == userId)
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResult<CommunityDto> { Data = data, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<CommunityDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var community = await _db.Communities
            .Where(c => c.Id == id && c.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .Select(c => new CommunityDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                IconName = c.IconName,
                CoverImageUrl = c.CoverImageUrl,
                MemberCount = c.MemberCount,
                IsActive = c.IsActive,
                IsMember = c.Members.Any(m => m.UserId == userId)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (community is null) throw new NotFoundException("Community", id);
        return community;
    }

    public async Task<CommunityDto> CreateAsync(CreateCommunityRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admins can create communities.");

        var slug = GenerateSlug(request.Name);
        var slugExists = await _db.Communities.AnyAsync(c => c.TenantId == _currentUser.TenantId && c.Slug == slug, cancellationToken);
        if (slugExists) throw new ConflictException($"A community with slug '{slug}' already exists.");

        var community = new Community
        {
            TenantId = _currentUser.TenantId,
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            IconName = request.IconName,
            CoverImageUrl = request.CoverImageUrl,
            MemberCount = 0,
            IsActive = true,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.Communities.Add(community);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(community.Id, cancellationToken);
    }

    public async Task<CommunityDto> UpdateAsync(Guid id, UpdateCommunityRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admins can update communities.");

        var community = await _db.Communities.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _currentUser.TenantId, cancellationToken);
        if (community is null) throw new NotFoundException("Community", id);

        if (!community.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase))
        {
            var slug = GenerateSlug(request.Name);
            var slugExists = await _db.Communities.AnyAsync(c => c.TenantId == _currentUser.TenantId && c.Slug == slug && c.Id != id, cancellationToken);
            if (slugExists) throw new ConflictException($"A community with slug '{slug}' already exists.");
            community.Name = request.Name;
            community.Slug = slug;
        }

        community.Description = request.Description;
        community.IconName = request.IconName;
        community.CoverImageUrl = request.CoverImageUrl;
        community.ModifiedOn = DateTime.UtcNow;
        community.ModifiedBy = _currentUser.UserId;
        community.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admins can delete communities.");

        var community = await _db.Communities.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _currentUser.TenantId, cancellationToken);
        if (community is null) throw new NotFoundException("Community", id);

        // Soft-delete: mark inactive
        community.IsActive = false;
        community.ModifiedOn = DateTime.UtcNow;
        community.ModifiedBy = _currentUser.UserId;
        community.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task JoinAsync(Guid communityId, CancellationToken cancellationToken)
    {
        var community = await _db.Communities.FirstOrDefaultAsync(c => c.Id == communityId && c.TenantId == _currentUser.TenantId && c.IsActive, cancellationToken);
        if (community is null) throw new NotFoundException("Community", communityId);

        var alreadyMember = await _db.CommunityMembers.AnyAsync(m => m.CommunityId == communityId && m.UserId == _currentUser.UserId, cancellationToken);
        if (alreadyMember) throw new ConflictException("You are already a member of this community.");

        var member = new CommunityMember
        {
            TenantId = _currentUser.TenantId,
            CommunityId = communityId,
            UserId = _currentUser.UserId,
            MemberRole = CommunityMemberRole.Member,
            JoinedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.CommunityMembers.Add(member);

        community.MemberCount++;
        community.ModifiedOn = DateTime.UtcNow;
        community.ModifiedBy = _currentUser.UserId;
        community.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task LeaveAsync(Guid communityId, CancellationToken cancellationToken)
    {
        var membership = await _db.CommunityMembers
            .FirstOrDefaultAsync(m => m.CommunityId == communityId && m.UserId == _currentUser.UserId, cancellationToken);
        if (membership is null) throw new NotFoundException("Community membership");

        var community = await _db.Communities.FirstOrDefaultAsync(c => c.Id == communityId && c.TenantId == _currentUser.TenantId, cancellationToken);
        if (community is null) throw new NotFoundException("Community", communityId);

        _db.CommunityMembers.Remove(membership);
        community.MemberCount = Math.Max(0, community.MemberCount - 1);
        community.ModifiedOn = DateTime.UtcNow;
        community.ModifiedBy = _currentUser.UserId;
        community.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateSlug(string name) =>
        name.ToLowerInvariant().Replace(' ', '-').Replace("--", "-").Trim('-');
}
