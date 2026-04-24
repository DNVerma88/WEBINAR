using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class SkillEndorsementService : ISkillEndorsementService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public SkillEndorsementService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SkillEndorsementDto> EndorseAsync(
        Guid sessionId, EndorseSkillRequest request, CancellationToken cancellationToken)
    {
        var hasAttended = await _db.SessionRegistrations
            .AnyAsync(r => r.SessionId == sessionId
                && r.ParticipantId == _currentUser.UserId
                && r.TenantId == _currentUser.TenantId
                && r.Status == RegistrationStatus.Attended, cancellationToken);

        if (!hasAttended)
            throw new ForbiddenException("You must have attended this session to endorse a skill.");

        if (request.EndorseeId == _currentUser.UserId)
            throw new BusinessRuleException("You cannot endorse yourself.");

        // B8: endorsements are only valid for the session's speaker
        var speakerId = await _db.Sessions
            .Where(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId)
            .Select(s => (Guid?)s.SpeakerId)
            .FirstOrDefaultAsync(cancellationToken);
        if (speakerId is null) throw new NotFoundException("Session", sessionId);
        if (request.EndorseeId != speakerId.Value)
            throw new BusinessRuleException("You can only endorse the session speaker.");

        var tagExists = await _db.Tags.AnyAsync(
            t => t.Id == request.TagId && t.TenantId == _currentUser.TenantId, cancellationToken);
        if (!tagExists) throw new NotFoundException("Tag", request.TagId);

        var alreadyEndorsed = await _db.SkillEndorsements.AnyAsync(
            e => e.TenantId == _currentUser.TenantId
                && e.EndorserId == _currentUser.UserId
                && e.EndorseeId == request.EndorseeId
                && e.TagId == request.TagId
                && e.SessionId == sessionId, cancellationToken);

        if (alreadyEndorsed) throw new ConflictException("You have already endorsed this skill for this session.");

        var endorsement = new SkillEndorsement
        {
            TenantId = _currentUser.TenantId,
            EndorserId = _currentUser.UserId,
            EndorseeId = request.EndorseeId,
            TagId = request.TagId,
            SessionId = sessionId,
            EndorsedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.SkillEndorsements.Add(endorsement);

        var profile = await _db.ContributorProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.EndorseeId && p.TenantId == _currentUser.TenantId, cancellationToken);

        if (profile is not null)
        {
            profile.EndorsementScore++;
            profile.ModifiedBy = _currentUser.UserId;
            profile.ModifiedOn = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var tag = await _db.Tags.FirstAsync(t => t.Id == request.TagId, cancellationToken);
        var endorser = await _db.Users.FirstAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        return new SkillEndorsementDto
        {
            Id = endorsement.Id,
            EndorserId = endorsement.EndorserId,
            EndorserName = endorser.FullName,
            EndorseeId = endorsement.EndorseeId,
            TagId = endorsement.TagId,
            TagName = tag.Name,
            SessionId = endorsement.SessionId,
            EndorsedAt = endorsement.EndorsedAt
        };
    }

    public async Task<PagedResult<SkillEndorsementDto>> GetEndorsementsForUserAsync(
        Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        pageSize = Math.Min(pageSize, 100);
        var query = _db.SkillEndorsements
            .Where(e => e.EndorseeId == userId && e.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query
            .OrderByDescending(e => e.EndorsedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new SkillEndorsementDto
            {
                Id = e.Id,
                EndorserId = e.EndorserId,
                EndorserName = e.Endorser != null ? e.Endorser.FullName : string.Empty,
                EndorseeId = e.EndorseeId,
                TagId = e.TagId,
                TagName = e.Tag != null ? e.Tag.Name : string.Empty,
                SessionId = e.SessionId,
                EndorsedAt = e.EndorsedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<SkillEndorsementDto>
        {
            Data = data, TotalCount = totalCount,
            PageNumber = pageNumber, PageSize = pageSize
        };
    }
}
