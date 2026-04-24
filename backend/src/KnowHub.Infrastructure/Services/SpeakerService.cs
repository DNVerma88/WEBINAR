using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Enums;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class SpeakerService : ISpeakerService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public SpeakerService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<SpeakerDto>> GetSpeakersAsync(GetSpeakersRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.UserId;

        var query = _db.ContributorProfiles
            .Where(cp => cp.TenantId == _currentUser.TenantId)
            .Join(_db.Users.Where(u => u.TenantId == _currentUser.TenantId && u.IsActive),
                cp => cp.UserId,
                u => u.Id,
                (cp, u) => new { Profile = cp, User = u })
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(x => x.User.FullName.Contains(request.SearchTerm)
                || (x.Profile.AreasOfExpertise != null && x.Profile.AreasOfExpertise.Contains(request.SearchTerm))
                || (x.Profile.TechnologiesKnown != null && x.Profile.TechnologiesKnown.Contains(request.SearchTerm)));

        if (!string.IsNullOrWhiteSpace(request.ExpertiseArea))
            query = query.Where(x => x.Profile.AreasOfExpertise != null && x.Profile.AreasOfExpertise.Contains(request.ExpertiseArea));

        if (!string.IsNullOrWhiteSpace(request.Technology))
            query = query.Where(x => x.Profile.TechnologiesKnown != null && x.Profile.TechnologiesKnown.Contains(request.Technology));

        var followedIds = _db.UserFollowers
            .Where(f => f.FollowerId == currentUserId && f.TenantId == _currentUser.TenantId)
            .Select(f => f.FollowedId);

        var sessionCounts = _db.Sessions
            .Where(s => s.TenantId == _currentUser.TenantId && s.Status != SessionStatus.Cancelled)
            .GroupBy(s => s.SpeakerId)
            .Select(g => new { SpeakerId = g.Key, Count = g.Count() });

        var (data, total) = await query
            .OrderByDescending(x => x.Profile.AverageRating)
            .ThenByDescending(x => sessionCounts.Where(sc => sc.SpeakerId == x.User.Id).Select(sc => sc.Count).FirstOrDefault())
            .Select(x => new SpeakerDto
            {
                UserId = x.User.Id,
                FullName = x.User.FullName,
                ProfilePhotoUrl = x.User.ProfilePhotoUrl,
                Department = x.User.Department,
                Designation = x.User.Designation,
                AreasOfExpertise = x.Profile.AreasOfExpertise,
                TechnologiesKnown = x.Profile.TechnologiesKnown,
                Bio = x.Profile.Bio,
                AverageRating = x.Profile.AverageRating,
                TotalSessionsDelivered = sessionCounts.Where(sc => sc.SpeakerId == x.User.Id).Select(sc => sc.Count).FirstOrDefault(),
                FollowerCount = x.Profile.FollowerCount,
                IsKnowledgeBroker = x.Profile.IsKnowledgeBroker,
                IsFollowedByCurrentUser = followedIds.Contains(x.User.Id),
                AvailableForMentoring = x.Profile.AvailableForMentoring
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResult<SpeakerDto> { Data = data, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<SpeakerDetailDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.UserId;
        var tenantId = _currentUser.TenantId;

        // Single JOIN query instead of two sequential round-trips
        var speaker = await _db.ContributorProfiles
            .Where(cp => cp.UserId == userId && cp.TenantId == tenantId)
            .Join(_db.Users.Where(u => u.Id == userId && u.TenantId == tenantId && u.IsActive),
                cp => cp.UserId,
                u => u.Id,
                (cp, u) => new { Profile = cp, User = u })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        if (speaker is null) throw new KnowHub.Domain.Exceptions.NotFoundException("Speaker", userId);

        var isFollowed = await _db.UserFollowers
            .AsNoTracking()
            .AnyAsync(f => f.FollowerId == currentUserId && f.FollowedId == userId && f.TenantId == tenantId, cancellationToken);

        var totalSessionsDelivered = await _db.Sessions
            .AsNoTracking()
            .CountAsync(s => s.SpeakerId == userId && s.TenantId == tenantId && s.Status != SessionStatus.Cancelled, cancellationToken);

        var recentSessions = await _db.Sessions
            .Where(s => s.SpeakerId == userId && s.TenantId == tenantId && s.Status != SessionStatus.Cancelled)
            .OrderByDescending(s => s.ScheduledAt)
            .Take(10)
            .AsNoTracking()
            .Select(s => new SpeakerSessionDto
            {
                Id = s.Id,
                Title = s.Title,
                ScheduledAt = s.ScheduledAt,
                CategoryName = s.Category != null ? s.Category.Name : null,
                DurationMinutes = s.DurationMinutes,
                AverageRating = _db.SessionRatings.Where(r => r.SessionId == s.Id).Any()
                    ? (decimal)_db.SessionRatings.Where(r => r.SessionId == s.Id).Average(r => r.SessionScore)
                    : 0m
            })
            .ToListAsync(cancellationToken);

        return new SpeakerDetailDto
        {
            UserId = speaker.User.Id,
            FullName = speaker.User.FullName,
            ProfilePhotoUrl = speaker.User.ProfilePhotoUrl,
            Department = speaker.User.Department,
            Designation = speaker.User.Designation,
            AreasOfExpertise = speaker.Profile.AreasOfExpertise,
            TechnologiesKnown = speaker.Profile.TechnologiesKnown,
            Bio = speaker.Profile.Bio,
            AverageRating = speaker.Profile.AverageRating,
            TotalSessionsDelivered = totalSessionsDelivered,
            FollowerCount = speaker.Profile.FollowerCount,
            IsKnowledgeBroker = speaker.Profile.IsKnowledgeBroker,
            IsFollowedByCurrentUser = isFollowed,
            AvailableForMentoring = speaker.Profile.AvailableForMentoring,
            RecentSessions = recentSessions
        };
    }
}
