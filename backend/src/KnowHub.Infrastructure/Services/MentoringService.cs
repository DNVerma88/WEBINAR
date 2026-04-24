using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class MentoringService : IMentoringService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly INotificationService _notificationService;

    public MentoringService(
        KnowHubDbContext db,
        ICurrentUserAccessor currentUser,
        INotificationService notificationService)
    {
        _db = db;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<MentorMenteeDto> RequestMentorAsync(
        RequestMentorRequest request, CancellationToken cancellationToken)
    {
        // API-27: replace full User + ContributorProfile entity loads with one AnyAsync check
        var isAvailable = await _db.ContributorProfiles
            .AnyAsync(p => p.UserId == request.MentorId
                        && p.TenantId == _currentUser.TenantId
                        && p.AvailableForMentoring, cancellationToken);
        if (!isAvailable)
            throw new BusinessRuleException("This user is not available for mentoring.");

        var mentorExists = await _db.Users
            .AnyAsync(u => u.Id == request.MentorId && u.TenantId == _currentUser.TenantId, cancellationToken);
        if (!mentorExists) throw new NotFoundException("User", request.MentorId);

        var pairing = new MentorMentee
        {
            TenantId = _currentUser.TenantId,
            MentorId = request.MentorId,
            MenteeId = _currentUser.UserId,
            Status = MentorMenteeStatus.Pending,
            GoalsText = request.GoalsText,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.MentorMentees.Add(pairing);
        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            request.MentorId, _currentUser.TenantId,
            NotificationType.General,
            "New Mentoring Request",
            "You have received a new mentoring request.",
            "MentorMentee", pairing.Id, cancellationToken);

        return await MapToDtoAsync(pairing, cancellationToken);
    }

    public async Task<PagedResult<MentorMenteeDto>> GetPairingsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.MentorMentees.Where(m => m.TenantId == _currentUser.TenantId).AsNoTracking();

        if (!_currentUser.IsAdminOrAbove)
        {
            query = query.Where(m =>
                m.MentorId == _currentUser.UserId || m.MenteeId == _currentUser.UserId);
        }

        var (data, total) = await query
            .OrderByDescending(m => m.CreatedDate)
            .Join(_db.Users, m => m.MentorId, u => u.Id,
                (m, mentor) => new { m, MentorName = mentor.FullName })
            .Join(_db.Users, x => x.m.MenteeId, u => u.Id,
                (x, mentee) => new MentorMenteeDto
                {
                    Id = x.m.Id, MentorId = x.m.MentorId, MentorName = x.MentorName,
                    MenteeId = x.m.MenteeId, MenteeName = mentee.FullName,
                    Status = x.m.Status, StartedAt = x.m.StartedAt, EndedAt = x.m.EndedAt,
                    GoalsText = x.m.GoalsText, MatchReason = x.m.MatchReason,
                    CreatedDate = x.m.CreatedDate
                })
            .ToPagedListAsync(pageNumber, pageSize, cancellationToken);

        return new PagedResult<MentorMenteeDto>
        {
            Data = data, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize
        };
    }

    public async Task<MentorMenteeDto> AcceptAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var pairing = await _db.MentorMentees
            .FirstOrDefaultAsync(m => m.Id == requestId && m.TenantId == _currentUser.TenantId, cancellationToken);

        if (pairing is null) throw new NotFoundException("MentorMentee", requestId);
        if (pairing.MentorId != _currentUser.UserId && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only the mentor may accept this request.");
        if (pairing.Status != MentorMenteeStatus.Pending)
            throw new BusinessRuleException("Only pending requests can be accepted.");

        pairing.Status = MentorMenteeStatus.Active;
        pairing.StartedAt = DateTime.UtcNow;
        pairing.ModifiedBy = _currentUser.UserId;
        pairing.ModifiedOn = DateTime.UtcNow;
        pairing.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            pairing.MenteeId, _currentUser.TenantId,
            NotificationType.General,
            "Mentoring Request Accepted",
            "Your mentoring request has been accepted.",
            "MentorMentee", pairing.Id, cancellationToken);

        return await MapToDtoAsync(pairing, cancellationToken);
    }

    public async Task<MentorMenteeDto> DeclineAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var pairing = await _db.MentorMentees
            .FirstOrDefaultAsync(m => m.Id == requestId && m.TenantId == _currentUser.TenantId, cancellationToken);

        if (pairing is null) throw new NotFoundException("MentorMentee", requestId);
        if (pairing.MentorId != _currentUser.UserId && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only the mentor may decline this request.");
        if (pairing.Status != MentorMenteeStatus.Pending)
            throw new BusinessRuleException("Only pending requests can be declined.");

        pairing.Status = MentorMenteeStatus.Declined;
        pairing.EndedAt = DateTime.UtcNow;
        pairing.ModifiedBy = _currentUser.UserId;
        pairing.ModifiedOn = DateTime.UtcNow;
        pairing.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(pairing, cancellationToken);
    }

    private async Task<MentorMenteeDto> MapToDtoAsync(MentorMentee pairing, CancellationToken ct)
    {
        // API-23: single query with ToDictionary instead of two sequential round-trips
        var names = await _db.Users
            .Where(u => u.Id == pairing.MentorId || u.Id == pairing.MenteeId)
            .AsNoTracking()
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        return new MentorMenteeDto
        {
            Id = pairing.Id,
            MentorId = pairing.MentorId, MentorName = names.GetValueOrDefault(pairing.MentorId) ?? string.Empty,
            MenteeId = pairing.MenteeId, MenteeName = names.GetValueOrDefault(pairing.MenteeId) ?? string.Empty,
            Status = pairing.Status,
            StartedAt = pairing.StartedAt, EndedAt = pairing.EndedAt,
            GoalsText = pairing.GoalsText, MatchReason = pairing.MatchReason,
            CreatedDate = pairing.CreatedDate
        };
    }
}
