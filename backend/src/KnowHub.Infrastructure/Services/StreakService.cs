using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class StreakService : IStreakService
{
    private static readonly HashSet<int> MilestoneDays = new() { 7, 30, 100 };

    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IXpService _xpService;

    public StreakService(KnowHubDbContext db, ICurrentUserAccessor currentUser, IXpService xpService)
    {
        _db = db;
        _currentUser = currentUser;
        _xpService = xpService;
    }

    public async Task<UserStreakDto> GetStreakAsync(Guid userId, CancellationToken cancellationToken)
    {
        var streak = await GetOrCreateStreakAsync(userId, _currentUser.TenantId, cancellationToken);
        return MapToDto(streak);
    }

    public async Task UpdateStreakAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var streak = await GetOrCreateStreakAsync(userId, tenantId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (streak.LastActivityDate == today) return;

        var isContinuation = streak.LastActivityDate == today.AddDays(-1);
        streak.LastActivityDate = today;
        streak.CurrentStreakDays = isContinuation ? streak.CurrentStreakDays + 1 : 1;

        if (streak.CurrentStreakDays > streak.LongestStreakDays)
            streak.LongestStreakDays = streak.CurrentStreakDays;

        streak.ModifiedBy = userId;
        streak.ModifiedOn = DateTime.UtcNow;
        streak.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);

        if (MilestoneDays.Contains(streak.CurrentStreakDays))
        {
            await _xpService.AwardXpAsync(new AwardXpRequest
            {
                UserId = userId,
                TenantId = tenantId,
                EventType = XpEventType.StreakMilestone,
                RelatedEntityType = "UserLearningStreak",
                RelatedEntityId = streak.Id
            }, cancellationToken);
        }
    }

    private async Task<UserLearningStreak> GetOrCreateStreakAsync(
        Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var streak = await _db.UserLearningStreaks
            .FirstOrDefaultAsync(s => s.UserId == userId && s.TenantId == tenantId, cancellationToken);

        if (streak is not null) return streak;

        streak = new UserLearningStreak
        {
            TenantId = tenantId,
            UserId = userId,
            CurrentStreakDays = 0,
            LongestStreakDays = 0,
            LastActivityDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedBy = userId,
            ModifiedBy = userId
        };

        _db.UserLearningStreaks.Add(streak);
        await _db.SaveChangesAsync(cancellationToken);
        return streak;
    }

    private static UserStreakDto MapToDto(UserLearningStreak s) => new()
    {
        UserId = s.UserId,
        CurrentStreakDays = s.CurrentStreakDays,
        LongestStreakDays = s.LongestStreakDays,
        LastActivityDate = s.LastActivityDate,
        StreakFrozenUntil = s.StreakFrozenUntil
    };
}
