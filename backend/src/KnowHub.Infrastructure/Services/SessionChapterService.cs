using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class SessionChapterService : ISessionChapterService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public SessionChapterService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<SessionChapterDto>> GetChaptersAsync(
        Guid sessionId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        pageSize = Math.Min(pageSize, 100);
        var sessionExists = await _db.Sessions
            .AnyAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (!sessionExists) throw new NotFoundException("Session", sessionId);

        var query = _db.SessionChapters
            .Where(c => c.SessionId == sessionId && c.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query
            .OrderBy(c => c.OrderSequence)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new SessionChapterDto
            {
                Id = c.Id, SessionId = c.SessionId,
                Title = c.Title, TimestampSeconds = c.TimestampSeconds,
                OrderSequence = c.OrderSequence
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<SessionChapterDto>
        {
            Data = data, TotalCount = totalCount,
            PageNumber = pageNumber, PageSize = pageSize
        };
    }

    public async Task<SessionChapterDto> AddChapterAsync(
        Guid sessionId, AddChapterRequest request, CancellationToken cancellationToken)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (session is null) throw new NotFoundException("Session", sessionId);

        if (!_currentUser.IsAdminOrAbove && session.SpeakerId != _currentUser.UserId)
            throw new ForbiddenException("Only the session speaker or an Admin may add chapters.");

        var chapter = new SessionChapter
        {
            TenantId = _currentUser.TenantId,
            SessionId = sessionId,
            Title = request.Title,
            TimestampSeconds = request.TimestampSeconds,
            OrderSequence = request.OrderSequence,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.SessionChapters.Add(chapter);
        await _db.SaveChangesAsync(cancellationToken);

        return new SessionChapterDto
        {
            Id = chapter.Id, SessionId = chapter.SessionId,
            Title = chapter.Title, TimestampSeconds = chapter.TimestampSeconds,
            OrderSequence = chapter.OrderSequence
        };
    }

    public async Task DeleteChapterAsync(Guid chapterId, CancellationToken cancellationToken)
    {
        var chapter = await _db.SessionChapters
            .Include(c => c.Session)
            .FirstOrDefaultAsync(c => c.Id == chapterId && c.TenantId == _currentUser.TenantId, cancellationToken);
        if (chapter is null) throw new NotFoundException("SessionChapter", chapterId);

        if (!_currentUser.IsAdminOrAbove && chapter.Session?.SpeakerId != _currentUser.UserId)
            throw new ForbiddenException("Only the session speaker or an Admin may delete chapters.");

        _db.SessionChapters.Remove(chapter);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
