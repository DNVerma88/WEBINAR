using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace KnowHub.Infrastructure.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IMemoryCache _cache;

    public LeaderboardService(KnowHubDbContext db, ICurrentUserAccessor currentUser, IMemoryCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<LeaderboardDto> GetLeaderboardAsync(
        LeaderboardType type, int? month, int? year, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        if (month.HasValue && year.HasValue)
        {
            var snapshot = await _db.LeaderboardSnapshots
                .Where(s => s.TenantId == tenantId
                    && s.LeaderboardType == type
                    && s.SnapshotMonth == month.Value
                    && s.SnapshotYear == year.Value)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (snapshot != null)
                return BuildFromSnapshot(snapshot, type, month.Value, year.Value);
        }

        // API-23: cache live leaderboard per (tenantId, type) for 5 minutes
        // to avoid running the full XP/session/rating GROUP BY aggregation on every page load
        var cacheKey = $"leaderboard:{tenantId}:{type}";
        if (!_cache.TryGetValue(cacheKey, out List<LeaderboardEntryDto>? entries) || entries is null)
        {
            entries = await ComputeLiveEntriesAsync(tenantId, type, cancellationToken);
            _cache.Set(cacheKey, entries, TimeSpan.FromMinutes(5));
        }

        return new LeaderboardDto
        {
            Type    = type.ToString(),
            Month   = month,
            Year    = year,
            Entries = entries
        };
    }

    private async Task<List<LeaderboardEntryDto>> ComputeLiveEntriesAsync(
        Guid tenantId, LeaderboardType type, CancellationToken cancellationToken)
    {
        return type switch
        {
            LeaderboardType.ByXp            => await ComputeByXpAsync(tenantId, cancellationToken),
            LeaderboardType.ByContributions => await ComputeByContributionsAsync(tenantId, cancellationToken),
            LeaderboardType.ByAttendance    => await ComputeByAttendanceAsync(tenantId, cancellationToken),
            LeaderboardType.ByRating        => await ComputeByRatingAsync(tenantId, cancellationToken),
            LeaderboardType.ByMentoring     => await ComputeByMentoringAsync(tenantId, cancellationToken),
            LeaderboardType.ByDepartment    => await ComputeByDepartmentAsync(tenantId, cancellationToken),
            _                               => new List<LeaderboardEntryDto>()
        };
    }

    private async Task<List<LeaderboardEntryDto>> ComputeByXpAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await _db.UserXpEvents
            .Where(e => e.TenantId == tenantId)
            .GroupBy(e => e.UserId)
            .Select(g => new { UserId = g.Key, Score = (decimal)g.Sum(e => e.XpAmount) })
            .OrderByDescending(x => x.Score)
            .Take(50)
            .Join(_db.Users, x => x.UserId, u => u.Id,
                (x, u) => new { x.UserId, x.Score, u.FullName, u.ProfilePhotoUrl })
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.Select((r, i) => new LeaderboardEntryDto
        {
            Rank = i + 1, UserId = r.UserId, DisplayName = r.FullName,
            Score = r.Score, AvatarUrl = r.ProfilePhotoUrl
        }).ToList();
    }

    private async Task<List<LeaderboardEntryDto>> ComputeByContributionsAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await _db.Sessions
            .Where(s => s.TenantId == tenantId && s.Status == SessionStatus.Completed)
            .GroupBy(s => s.SpeakerId)
            .Select(g => new { UserId = g.Key, Score = (decimal)g.Count() })
            .OrderByDescending(x => x.Score)
            .Take(50)
            .Join(_db.Users, x => x.UserId, u => u.Id,
                (x, u) => new { x.UserId, x.Score, u.FullName, u.ProfilePhotoUrl })
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.Select((r, i) => new LeaderboardEntryDto
        {
            Rank = i + 1, UserId = r.UserId, DisplayName = r.FullName,
            Score = r.Score, AvatarUrl = r.ProfilePhotoUrl
        }).ToList();
    }

    private async Task<List<LeaderboardEntryDto>> ComputeByAttendanceAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await _db.SessionRegistrations
            .Where(r => r.TenantId == tenantId && r.Status == RegistrationStatus.Attended)
            .GroupBy(r => r.ParticipantId)
            .Select(g => new { UserId = g.Key, Score = (decimal)g.Count() })
            .OrderByDescending(x => x.Score)
            .Take(50)
            .Join(_db.Users, x => x.UserId, u => u.Id,
                (x, u) => new { x.UserId, x.Score, u.FullName, u.ProfilePhotoUrl })
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.Select((r, i) => new LeaderboardEntryDto
        {
            Rank = i + 1, UserId = r.UserId, DisplayName = r.FullName,
            Score = r.Score, AvatarUrl = r.ProfilePhotoUrl
        }).ToList();
    }

    private async Task<List<LeaderboardEntryDto>> ComputeByRatingAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await _db.SessionRatings
            .Where(r => r.TenantId == tenantId)
            .Join(_db.Sessions, r => r.SessionId, s => s.Id, (r, s) => new { r, s.SpeakerId })
            .GroupBy(x => x.SpeakerId)
            .Select(g => new { UserId = g.Key, Score = g.Average(x => (decimal)x.r.SpeakerScore) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(50)
            .Join(_db.Users, x => x.UserId, u => u.Id,
                (x, u) => new { x.UserId, x.Score, u.FullName, u.ProfilePhotoUrl })
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.Select((r, i) => new LeaderboardEntryDto
        {
            Rank = i + 1, UserId = r.UserId, DisplayName = r.FullName,
            Score = Math.Round(r.Score, 2), AvatarUrl = r.ProfilePhotoUrl
        }).ToList();
    }

    private async Task<List<LeaderboardEntryDto>> ComputeByMentoringAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await _db.MentorMentees
            .Where(m => m.TenantId == tenantId && m.Status == MentorMenteeStatus.Active)
            .GroupBy(m => m.MentorId)
            .Select(g => new { UserId = g.Key, Score = (decimal)g.Count() })
            .OrderByDescending(x => x.Score)
            .Take(50)
            .Join(_db.Users, x => x.UserId, u => u.Id,
                (x, u) => new { x.UserId, x.Score, u.FullName, u.ProfilePhotoUrl })
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.Select((r, i) => new LeaderboardEntryDto
        {
            Rank = i + 1, UserId = r.UserId, DisplayName = r.FullName,
            Score = r.Score, AvatarUrl = r.ProfilePhotoUrl
        }).ToList();
    }

    private async Task<List<LeaderboardEntryDto>> ComputeByDepartmentAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await _db.UserXpEvents
            .Where(e => e.TenantId == tenantId)
            .Join(_db.Users, e => e.UserId, u => u.Id, (e, u) => new { e.XpAmount, u.Department })
            .GroupBy(x => x.Department)
            .Select(g => new { Department = g.Key ?? "Unknown", Score = (decimal)g.Sum(x => x.XpAmount) })
            .OrderByDescending(x => x.Score)
            .Take(50)
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.Select((r, i) => new LeaderboardEntryDto
        {
            Rank = i + 1, UserId = Guid.Empty, DisplayName = r.Department,
            Score = r.Score, AvatarUrl = null
        }).ToList();
    }

    private static LeaderboardDto BuildFromSnapshot(
        LeaderboardSnapshot snapshot, LeaderboardType type, int month, int year)
    {
        var entries = JsonSerializer.Deserialize<List<LeaderboardEntryDto>>(snapshot.Entries)
            ?? new List<LeaderboardEntryDto>();

        return new LeaderboardDto
        {
            Type = type.ToString(), Month = month, Year = year, Entries = entries
        };
    }
}
