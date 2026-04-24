using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class XpService : IXpService
{
    // XP amounts are hardcoded server-side — never accepted from client input (OWASP A04)
    private static readonly Dictionary<XpEventType, int> XpAmounts = new()
    {
        [XpEventType.AttendSession]           = 10,
        [XpEventType.SubmitProposal]          = 5,
        [XpEventType.ProposalApproved]        = 20,
        [XpEventType.DeliverSession]          = 50,
        [XpEventType.FiveStarRating]          = 25,
        [XpEventType.AnswerKnowledgeRequest]  = 15,
        [XpEventType.UploadAsset]             = 10,
        [XpEventType.CommentLiked]            = 2,
        [XpEventType.LearningPathCompleted]   = 30,
        [XpEventType.BadgeAwarded]            = 0,
        [XpEventType.CompleteQuiz]            = 15,
        [XpEventType.StreakMilestone]         = 20,
        [XpEventType.ReferContributor]        = 10,
    };

    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public XpService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<UserXpDto> GetUserXpAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var userExists = await _db.Users.AnyAsync(u => u.Id == userId && u.TenantId == tenantId, cancellationToken);
        if (!userExists) throw new NotFoundException("User", userId);

        // API-08: separate SQL queries — Sum across all rows, Take(20) for the recent list
        // avoids loading the entire XP event history into memory
        var totalXp = await _db.UserXpEvents
            .Where(e => e.UserId == userId && e.TenantId == tenantId)
            .SumAsync(e => e.XpAmount, cancellationToken);

        var recentEvents = await _db.UserXpEvents
            .Where(e => e.UserId == userId && e.TenantId == tenantId)
            .OrderByDescending(e => e.EarnedAt)
            .Take(20)
            .AsNoTracking()
            .Select(e => new XpEventDto
            {
                EventType = e.EventType,
                XpAmount = e.XpAmount,
                EarnedAt = e.EarnedAt,
                RelatedEntityType = e.RelatedEntityType,
                RelatedEntityId = e.RelatedEntityId
            })
            .ToListAsync(cancellationToken);

        return new UserXpDto { UserId = userId, TotalXp = totalXp, RecentEvents = recentEvents };
    }

    public async Task AwardXpAsync(AwardXpRequest request, CancellationToken cancellationToken)
    {
        if (!XpAmounts.TryGetValue(request.EventType, out var amount)) return;

        // Idempotency: prevent duplicate XP awards for the same event+entity combination
        if (request.RelatedEntityId.HasValue)
        {
            var alreadyAwarded = await _db.UserXpEvents.AnyAsync(
                e => e.TenantId == request.TenantId
                  && e.UserId == request.UserId
                  && e.EventType == request.EventType
                  && e.RelatedEntityId == request.RelatedEntityId,
                cancellationToken);
            if (alreadyAwarded) return;
        }

        var xpEvent = new UserXpEvent
        {
            TenantId    = request.TenantId,
            UserId      = request.UserId,
            EventType   = request.EventType,
            XpAmount    = amount,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId   = request.RelatedEntityId,
            EarnedAt    = DateTime.UtcNow,
            CreatedBy   = request.UserId,
            ModifiedBy  = request.UserId,
        };

        _db.UserXpEvents.Add(xpEvent);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
