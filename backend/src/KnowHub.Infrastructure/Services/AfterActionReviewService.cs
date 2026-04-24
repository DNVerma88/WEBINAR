using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class AfterActionReviewService : IAfterActionReviewService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public AfterActionReviewService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AfterActionReviewDto> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var aar = await _db.AfterActionReviews
            .Include(a => a.Author)
            .Where(a => a.SessionId == sessionId && a.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (aar is null) throw new NotFoundException("AfterActionReview for session", sessionId);

        if (!aar.IsPublished && !_currentUser.IsAdminOrAbove && aar.AuthorId != _currentUser.UserId)
            throw new ForbiddenException("This after-action review is not published.");

        return MapToDto(aar);
    }

    public async Task<AfterActionReviewDto> CreateAsync(
        Guid sessionId, CreateAarRequest request, CancellationToken cancellationToken)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (session is null) throw new NotFoundException("Session", sessionId);

        if (!_currentUser.IsAdminOrAbove && session.SpeakerId != _currentUser.UserId)
            throw new ForbiddenException("Only the session speaker or an Admin may create an AAR.");

        var exists = await _db.AfterActionReviews
            .AnyAsync(a => a.SessionId == sessionId && a.TenantId == _currentUser.TenantId, cancellationToken);
        if (exists) throw new ConflictException("An after-action review already exists for this session.");

        var aar = new AfterActionReview
        {
            TenantId = _currentUser.TenantId,
            SessionId = sessionId,
            AuthorId = _currentUser.UserId,
            WhatWasPlanned = request.WhatWasPlanned,
            WhatHappened = request.WhatHappened,
            WhatWentWell = request.WhatWentWell,
            WhatToImprove = request.WhatToImprove,
            KeyLessonsLearned = request.KeyLessonsLearned,
            IsPublished = request.IsPublished,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.AfterActionReviews.Add(aar);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(aar);
    }

    public async Task<AfterActionReviewDto> UpdateAsync(
        Guid sessionId, UpdateAarRequest request, CancellationToken cancellationToken)
    {
        var aar = await _db.AfterActionReviews
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.SessionId == sessionId && a.TenantId == _currentUser.TenantId, cancellationToken);
        if (aar is null) throw new NotFoundException("AfterActionReview for session", sessionId);

        if (!_currentUser.IsAdminOrAbove && aar.AuthorId != _currentUser.UserId)
            throw new ForbiddenException("Only the AAR author or an Admin may update it.");

        aar.WhatWasPlanned = request.WhatWasPlanned;
        aar.WhatHappened = request.WhatHappened;
        aar.WhatWentWell = request.WhatWentWell;
        aar.WhatToImprove = request.WhatToImprove;
        aar.KeyLessonsLearned = request.KeyLessonsLearned;
        aar.IsPublished = request.IsPublished;
        aar.ModifiedBy = _currentUser.UserId;
        aar.ModifiedOn = DateTime.UtcNow;
        aar.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);
        return MapToDto(aar);
    }

    private static AfterActionReviewDto MapToDto(AfterActionReview a) => new()
    {
        Id = a.Id, SessionId = a.SessionId, AuthorId = a.AuthorId,
        AuthorName = a.Author?.FullName ?? string.Empty,
        WhatWasPlanned = a.WhatWasPlanned, WhatHappened = a.WhatHappened,
        WhatWentWell = a.WhatWentWell, WhatToImprove = a.WhatToImprove,
        KeyLessonsLearned = a.KeyLessonsLearned, IsPublished = a.IsPublished,
        CreatedDate = a.CreatedDate
    };
}
