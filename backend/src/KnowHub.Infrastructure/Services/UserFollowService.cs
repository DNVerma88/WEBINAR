using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class UserFollowService : IUserFollowService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IDistributedCommunityCache _cache;

    public UserFollowService(KnowHubDbContext db, ICurrentUserAccessor currentUser, IDistributedCommunityCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<bool> ToggleFollowUserAsync(Guid targetUserId, CancellationToken ct)
    {
        var tenantId  = _currentUser.TenantId;
        var followerId = _currentUser.UserId;

        if (followerId == targetUserId)
            throw new InvalidOperationException("Users cannot follow themselves.");

        var targetExists = await _db.Users
            .AnyAsync(u => u.Id == targetUserId && u.TenantId == tenantId, ct);

        if (!targetExists)
            throw new NotFoundException("User", targetUserId);

        var existing = await _db.UserTagFollows
            .Where(f => f.TenantId == tenantId
                        && f.FollowerId == followerId
                        && f.FollowedUserId == targetUserId)
            .FirstOrDefaultAsync(ct);

        bool isFollowing;
        if (existing is null)
        {
            _db.UserTagFollows.Add(new UserTagFollow
            {
                TenantId       = tenantId,
                FollowerId     = followerId,
                FollowedUserId = targetUserId,
                CreatedBy      = followerId,
                ModifiedBy     = followerId
            });
            isFollowing = true;
        }
        else
        {
            _db.UserTagFollows.Remove(existing);
            isFollowing = false;
        }

        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateFeedAsync(tenantId, followerId, ct);

        return isFollowing;
    }

    public async Task<PagedResult<UserSummaryDto>> GetFollowersAsync(Guid userId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var currentUserId = _currentUser.UserId;
        pageSize = Math.Clamp(pageSize, 1, 100);
        pageNumber = Math.Max(1, pageNumber);

        var followingIds = await _db.UserTagFollows
            .Where(f => f.TenantId == tenantId && f.FollowerId == currentUserId && f.FollowedUserId.HasValue)
            .Select(f => f.FollowedUserId!.Value)
            .ToListAsync(ct);

        var query = _db.UserTagFollows
            .Where(f => f.TenantId == tenantId
                        && f.FollowedUserId == userId
                        && f.FollowedUserId.HasValue);

        var total = await query.CountAsync(ct);
        var followers = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(f => f.Follower)
            .AsNoTracking()
            .Select(f => new UserSummaryDto
            {
                Id                     = f.FollowerId,
                FullName               = f.Follower!.FullName,
                AvatarUrl              = f.Follower.ProfilePhotoUrl,
                IsFollowedByCurrentUser = followingIds.Contains(f.FollowerId)
            })
            .ToListAsync(ct);

        return new PagedResult<UserSummaryDto> { Data = followers, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<PagedResult<UserSummaryDto>> GetFollowingAsync(Guid userId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var currentUserId = _currentUser.UserId;
        pageSize = Math.Clamp(pageSize, 1, 100);
        pageNumber = Math.Max(1, pageNumber);

        var myFollowingIds = await _db.UserTagFollows
            .Where(f => f.TenantId == tenantId && f.FollowerId == currentUserId && f.FollowedUserId.HasValue)
            .Select(f => f.FollowedUserId!.Value)
            .ToListAsync(ct);

        var query = _db.UserTagFollows
            .Where(f => f.TenantId == tenantId
                        && f.FollowerId == userId
                        && f.FollowedUserId.HasValue);

        var total = await query.CountAsync(ct);
        var following = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(f => f.FollowedUser)
            .AsNoTracking()
            .Select(f => new UserSummaryDto
            {
                Id                     = f.FollowedUserId!.Value,
                FullName               = f.FollowedUser!.FullName,
                AvatarUrl              = f.FollowedUser.ProfilePhotoUrl,
                IsFollowedByCurrentUser = myFollowingIds.Contains(f.FollowedUserId!.Value)
            })
            .ToListAsync(ct);

        return new PagedResult<UserSummaryDto> { Data = following, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }
}

