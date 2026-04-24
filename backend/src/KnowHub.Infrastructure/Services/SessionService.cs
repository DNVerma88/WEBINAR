using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class SessionService : ISessionService
{
    private static readonly HashSet<string> AllowedMeetingDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "teams.microsoft.com", "zoom.us", "meet.google.com", "webex.com", "goto.com", "bluejeans.com"
    };

    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly INotificationService _notificationService;

    public SessionService(KnowHubDbContext db, ICurrentUserAccessor currentUser, INotificationService notificationService)
    {
        _db = db;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<SessionDto>> GetSessionsAsync(GetSessionsRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Sessions
            .Where(s => s.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (request.CategoryId.HasValue) query = query.Where(s => s.CategoryId == request.CategoryId.Value);
        if (request.SpeakerId.HasValue) query = query.Where(s => s.SpeakerId == request.SpeakerId.Value);
        if (request.Format.HasValue) query = query.Where(s => s.Format == request.Format.Value);
        if (request.DifficultyLevel.HasValue) query = query.Where(s => s.DifficultyLevel == request.DifficultyLevel.Value);
        if (request.Status.HasValue) query = query.Where(s => s.Status == request.Status.Value);
        if (request.FromDate.HasValue) query = query.Where(s => s.ScheduledAt >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(s => s.ScheduledAt <= request.ToDate.Value);
        if (request.TagId.HasValue) query = query.Where(s => s.SessionTags.Any(t => t.TagId == request.TagId.Value));
        if (!string.IsNullOrWhiteSpace(request.Department))
            query = query.Where(s => s.Speaker.Department == request.Department);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(s => s.Title.Contains(request.SearchTerm) || (s.Description != null && s.Description.Contains(request.SearchTerm)));

        // API-19: use an inline Select projection — EF Core cannot translate a static method
        // call inside a LINQ expression tree; it would throw or do client-side evaluation.
        var (data, total) = await query
            .OrderBy(s => s.ScheduledAt)
            .Select(s => new SessionDto
            {
                Id                = s.Id,
                ProposalId        = s.ProposalId,
                SpeakerId         = s.SpeakerId,
                SpeakerName       = s.Speaker != null ? s.Speaker.FullName : string.Empty,
                SpeakerPhotoUrl   = s.Speaker != null ? s.Speaker.ProfilePhotoUrl : null,
                Title             = s.Title,
                CategoryId        = s.CategoryId,
                CategoryName      = s.Category != null ? s.Category.Name : string.Empty,
                Format            = s.Format,
                DifficultyLevel   = s.DifficultyLevel,
                ScheduledAt       = s.ScheduledAt,
                DurationMinutes   = s.DurationMinutes,
                MeetingLink       = s.MeetingLink,
                MeetingPlatform   = s.MeetingPlatform,
                ParticipantLimit  = s.ParticipantLimit,
                RegisteredCount   = s.Registrations.Count(r => r.Status == RegistrationStatus.Registered),
                Status            = s.Status,
                IsPublic          = s.IsPublic,
                RecordingUrl      = s.RecordingUrl,
                Description       = s.Description,
                Tags              = s.SessionTags.Where(t => t.Tag != null).Select(t => t.Tag.Name).ToList(),
                RecordVersion     = s.RecordVersion
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResult<SessionDto> { Data = data, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<SessionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // API-13: AsSplitQuery prevents the Cartesian product from
        // SessionTags × Registrations (10 tags × 200 regs = 2,000 result rows without it)
        var session = await _db.Sessions
            .Where(s => s.Id == id && s.TenantId == _currentUser.TenantId)
            .Include(s => s.Speaker)
            .Include(s => s.Category)
            .Include(s => s.SessionTags).ThenInclude(t => t.Tag)
            .Include(s => s.Registrations)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null) throw new NotFoundException("Session", id);
        return MapToDto(session);
    }

    public async Task<SessionDto> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove && !_currentUser.IsInRole(UserRole.KnowledgeTeam))
            throw new ForbiddenException("Only Admin or KnowledgeTeam members can create sessions.");

        ValidateMeetingLink(request.MeetingLink);

        var proposal = await _db.SessionProposals
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (proposal is null) throw new NotFoundException("SessionProposal", request.ProposalId);
        if (proposal.Status != ProposalStatus.Published)
            throw new BusinessRuleException("Only published proposals can have sessions created.");

        var session = new Session
        {
            TenantId = _currentUser.TenantId,
            ProposalId = request.ProposalId,
            SpeakerId = request.SpeakerId ?? proposal.ProposerId,
            Title = proposal.Title,
            CategoryId = proposal.CategoryId,
            Format = proposal.Format,
            DifficultyLevel = proposal.DifficultyLevel,
            Description = proposal.Description,
            ScheduledAt = DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Utc),
            DurationMinutes = request.DurationMinutes,
            MeetingLink = request.MeetingLink,
            MeetingPlatform = request.MeetingPlatform,
            ParticipantLimit = request.ParticipantLimit,
            IsPublic = request.IsPublic,
            Status = SessionStatus.Scheduled,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.Sessions.Add(session);

        if (request.TagIds.Count > 0)
        {
            var sessionTags = request.TagIds.Select(tagId => new SessionTag
            {
                TenantId = _currentUser.TenantId,
                SessionId = session.Id,
                TagId = tagId,
                CreatedBy = _currentUser.UserId,
                ModifiedBy = _currentUser.UserId
            }).ToList();
            _db.SessionTags.AddRange(sessionTags);
        }

        proposal.Status = ProposalStatus.Scheduled;
        proposal.ModifiedOn = DateTime.UtcNow;
        proposal.ModifiedBy = _currentUser.UserId;
        proposal.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(session.Id, cancellationToken);
    }

    public async Task<SessionDto> UpdateAsync(Guid id, UpdateSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await _db.Sessions
            .Include(s => s.SessionTags)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (session is null) throw new NotFoundException("Session", id);

        EnsureCanManageSession(session);

        if (session.RecordVersion != request.RecordVersion) throw new ConflictException("Session was modified by another session.");
        ValidateMeetingLink(request.MeetingLink);
        // B19: recording URL must be HTTPS to prevent SSRF / phishing via stored URLs
        if (!string.IsNullOrEmpty(request.RecordingUrl))
            ValidateHttpsUrl(request.RecordingUrl, "RecordingUrl");

        session.ScheduledAt = DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Utc);
        session.DurationMinutes = request.DurationMinutes;
        session.MeetingLink = request.MeetingLink;
        session.MeetingPlatform = request.MeetingPlatform;
        session.ParticipantLimit = request.ParticipantLimit;
        session.IsPublic = request.IsPublic;
        session.RecordingUrl = request.RecordingUrl;
        if (request.SpeakerId.HasValue) session.SpeakerId = request.SpeakerId.Value;
        session.ModifiedOn = DateTime.UtcNow;
        session.ModifiedBy = _currentUser.UserId;
        session.RecordVersion++;

        _db.SessionTags.RemoveRange(session.SessionTags);
        var newTags = request.TagIds.Select(tagId => new SessionTag
        {
            TenantId = _currentUser.TenantId,
            SessionId = id,
            TagId = tagId,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        }).ToList();
        _db.SessionTags.AddRange(newTags);

        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SessionDto> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await _db.Sessions
            .Include(s => s.Registrations)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (session is null) throw new NotFoundException("Session", id);
        if (session.Status == SessionStatus.Cancelled) throw new BusinessRuleException("Session is already cancelled.");
        if (session.Status == SessionStatus.Completed) throw new BusinessRuleException("Cannot cancel a completed session.");

        EnsureCanManageSession(session);

        session.Status = SessionStatus.Cancelled;
        session.ModifiedOn = DateTime.UtcNow;
        session.ModifiedBy = _currentUser.UserId;
        session.RecordVersion++;

        // API-01: collect all notification payloads first, then persist in a single
        // SaveChangesAsync call via SendBulkAsync — avoids N×1 DB round-trips in the loop
        var notificationPayloads = session.Registrations
            .Where(r => r.Status == RegistrationStatus.Registered)
            .Select(reg => (
                UserId: reg.ParticipantId,
                TenantId: _currentUser.TenantId,
                Type: NotificationType.SessionCancelled,
                Title: $"Session Cancelled: {session.Title}",
                Body: "The session you registered for has been cancelled.",
                RelatedEntityType: (string?)"Session",
                RelatedEntityId: (Guid?)id
            ))
            .ToList();

        await _db.SaveChangesAsync(cancellationToken);

        if (notificationPayloads.Count > 0)
            await _notificationService.SendBulkAsync(notificationPayloads, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SessionDto> CompleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await _db.Sessions
            .Include(s => s.Registrations)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (session is null) throw new NotFoundException("Session", id);
        if (session.Status == SessionStatus.Completed) throw new BusinessRuleException("Session is already completed.");
        if (session.Status == SessionStatus.Cancelled) throw new BusinessRuleException("Cannot complete a cancelled session.");

        EnsureCanManageSession(session);

        session.Status = SessionStatus.Completed;
        session.ModifiedOn = DateTime.UtcNow;
        session.ModifiedBy = _currentUser.UserId;
        session.RecordVersion++;

        // Update attendance status for all registered participants
        foreach (var reg in session.Registrations.Where(r => r.Status == RegistrationStatus.Registered))
        {
            reg.Status = RegistrationStatus.Attended;
            reg.ModifiedOn = DateTime.UtcNow;
            reg.ModifiedBy = _currentUser.UserId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SessionRegistrationDto> RegisterAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        // API-17: AsNoTracking — this entity is only read for validation, never written back
        var session = await _db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (session is null) throw new NotFoundException("Session", sessionId);
        if (session.Status != SessionStatus.Scheduled) throw new BusinessRuleException("Cannot register for a session that is not scheduled.");

        // B5: the speaker cannot register for their own session
        if (session.SpeakerId == _currentUser.UserId)
            throw new BusinessRuleException("The session speaker cannot register for their own session.");

        // B9: private sessions are invite-only
        if (!session.IsPublic && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("This session is not open for public registration.");

        var existing = await _db.SessionRegistrations
            .AnyAsync(r => r.SessionId == sessionId && r.ParticipantId == _currentUser.UserId && r.Status != RegistrationStatus.Cancelled, cancellationToken);
        if (existing) throw new ConflictException("You are already registered for this session.");

        var registeredCount = await _db.SessionRegistrations
            .CountAsync(r => r.SessionId == sessionId && r.Status == RegistrationStatus.Registered, cancellationToken);

        int? waitlistPosition = null;
        var status = RegistrationStatus.Registered;

        if (session.ParticipantLimit.HasValue && registeredCount >= session.ParticipantLimit.Value)
        {
            var maxWaitlist = await _db.SessionRegistrations
                .Where(r => r.SessionId == sessionId && r.Status == RegistrationStatus.Waitlisted)
                .MaxAsync(r => (int?)r.WaitlistPosition, cancellationToken);
            waitlistPosition = (maxWaitlist ?? 0) + 1;
            status = RegistrationStatus.Waitlisted;
        }

        var registration = new SessionRegistration
        {
            TenantId = _currentUser.TenantId,
            SessionId = sessionId,
            ParticipantId = _currentUser.UserId,
            WaitlistPosition = waitlistPosition,
            RegisteredAt = DateTime.UtcNow,
            Status = status,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.SessionRegistrations.Add(registration);
        await _db.SaveChangesAsync(cancellationToken);

        var userName = await _db.Users.Where(u => u.Id == _currentUser.UserId).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        return new SessionRegistrationDto
        {
            Id = registration.Id,
            SessionId = registration.SessionId,
            ParticipantId = registration.ParticipantId,
            ParticipantName = userName,
            WaitlistPosition = registration.WaitlistPosition,
            RegisteredAt = registration.RegisteredAt,
            Status = registration.Status
        };
    }

    public async Task CancelRegistrationAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var registration = await _db.SessionRegistrations
            .FirstOrDefaultAsync(r => r.SessionId == sessionId && r.ParticipantId == _currentUser.UserId && r.Status != RegistrationStatus.Cancelled, cancellationToken);
        if (registration is null) throw new NotFoundException("SessionRegistration for this session");

        var wasRegistered = registration.Status == RegistrationStatus.Registered;
        registration.Status = RegistrationStatus.Cancelled;
        registration.ModifiedOn = DateTime.UtcNow;
        registration.ModifiedBy = _currentUser.UserId;
        registration.RecordVersion++;

        if (wasRegistered)
            await PromoteFirstWaitlistedAsync(sessionId, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SessionMaterialDto>> GetMaterialsAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var sessionExists = await _db.Sessions.AnyAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (!sessionExists) throw new NotFoundException("Session", sessionId);

        return await _db.SessionMaterials
            .Where(m => m.SessionId == sessionId && m.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .OrderBy(m => m.CreatedDate)
            .Take(200)
            .Select(m => new SessionMaterialDto
            {
                Id = m.Id,
                SessionId = m.SessionId,
                ProposalId = m.ProposalId,
                MaterialType = m.MaterialType,
                Title = m.Title,
                Url = m.Url,
                FileSizeBytes = m.FileSizeBytes
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SessionMaterialDto> AddMaterialAsync(Guid sessionId, AddSessionMaterialRequest request, CancellationToken cancellationToken)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (session is null) throw new NotFoundException("Session", sessionId);
        EnsureCanManageSession(session);
        ValidateMaterialUrl(request.Url);

        var material = new SessionMaterial
        {
            TenantId = _currentUser.TenantId,
            SessionId = sessionId,
            MaterialType = request.MaterialType,
            Title = request.Title,
            Url = request.Url,
            FileSizeBytes = request.FileSizeBytes,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.SessionMaterials.Add(material);
        await _db.SaveChangesAsync(cancellationToken);

        return new SessionMaterialDto
        {
            Id = material.Id,
            SessionId = material.SessionId,
            MaterialType = material.MaterialType,
            Title = material.Title,
            Url = material.Url,
            FileSizeBytes = material.FileSizeBytes
        };
    }

    public async Task<SessionRatingSummaryDto> GetRatingsSummaryAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var sessionExists = await _db.Sessions.AnyAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (!sessionExists) throw new NotFoundException("Session", sessionId);

        // API-15: compute averages in SQL via GROUP BY — avoids loading all rating rows to memory
        var summary = await _db.SessionRatings
            .Where(r => r.SessionId == sessionId && r.TenantId == _currentUser.TenantId)
            .GroupBy(_ => 0)
            .Select(g => new
            {
                AvgSession = (double?)g.Average(r => (double)r.SessionScore),
                AvgSpeaker = (double?)g.Average(r => (double)r.SpeakerScore),
                Count = g.Count()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (summary is null || summary.Count == 0)
            return new SessionRatingSummaryDto { AverageSessionScore = 0, AverageSpeakerScore = 0, TotalRatings = 0 };

        return new SessionRatingSummaryDto
        {
            AverageSessionScore = summary.AvgSession ?? 0,
            AverageSpeakerScore = summary.AvgSpeaker ?? 0,
            TotalRatings = summary.Count
        };
    }

    public async Task<SessionRatingDto> SubmitRatingAsync(Guid sessionId, SubmitSessionRatingRequest request, CancellationToken cancellationToken)
    {
        // API-17: AsNoTracking — session is only read for SpeakerId and Status checks here
        var session = await _db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (session is null) throw new NotFoundException("Session", sessionId);
        if (session.Status != SessionStatus.Completed) throw new BusinessRuleException("Ratings can only be submitted for completed sessions.");

        // B5: the speaker cannot inflate their own ratings
        if (session.SpeakerId == _currentUser.UserId)
            throw new BusinessRuleException("The session speaker cannot rate their own session.");

        var hasAttended = await _db.SessionRegistrations.AnyAsync(r =>
            r.SessionId == sessionId && r.ParticipantId == _currentUser.UserId && r.Status == RegistrationStatus.Attended, cancellationToken);
        if (!hasAttended) throw new ForbiddenException("Only attendees can rate a session.");

        var alreadyRated = await _db.SessionRatings.AnyAsync(r => r.SessionId == sessionId && r.RaterId == _currentUser.UserId, cancellationToken);
        if (alreadyRated) throw new ConflictException("You have already submitted a rating for this session.");

        var rating = new SessionRating
        {
            TenantId = _currentUser.TenantId,
            SessionId = sessionId,
            RaterId = _currentUser.UserId,
            SessionScore = request.SessionScore,
            SpeakerScore = request.SpeakerScore,
            FeedbackText = request.FeedbackText,
            NextSessionSuggestion = request.NextSessionSuggestion,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.SessionRatings.Add(rating);

        // Keep ContributorProfile.AverageRating in sync with new rating
        // API-02: compute average after adding the rating, then save both in one SaveChangesAsync
        var profile = await _db.ContributorProfiles
            .FirstOrDefaultAsync(p => p.UserId == session.SpeakerId && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (profile is not null)
        {
            var avgSpeaker = await _db.SessionRatings
                .Where(r => r.Session.SpeakerId == session.SpeakerId && r.TenantId == _currentUser.TenantId)
                .AverageAsync(r => (double)r.SpeakerScore, cancellationToken);
            profile.AverageRating = (decimal)Math.Round(avgSpeaker, 2);
            profile.ModifiedBy  = _currentUser.UserId;
            profile.ModifiedOn  = DateTime.UtcNow;
        }

        // API-02: single SaveChangesAsync commits both the rating and the updated profile
        await _db.SaveChangesAsync(cancellationToken);

        var raterName = await _db.Users.Where(u => u.Id == _currentUser.UserId).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        return new SessionRatingDto
        {
            Id = rating.Id,
            SessionId = rating.SessionId,
            RaterId = rating.RaterId,
            RaterName = raterName,
            SessionScore = rating.SessionScore,
            SpeakerScore = rating.SpeakerScore,
            FeedbackText = rating.FeedbackText,
            NextSessionSuggestion = rating.NextSessionSuggestion,
            CreatedDate = DateTime.UtcNow
        };
    }

    private async Task PromoteFirstWaitlistedAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var first = await _db.SessionRegistrations
            .Where(r => r.SessionId == sessionId && r.Status == RegistrationStatus.Waitlisted)
            .OrderBy(r => r.WaitlistPosition)
            .FirstOrDefaultAsync(cancellationToken);

        if (first is null) return;

        first.Status = RegistrationStatus.Registered;
        first.WaitlistPosition = null;
        first.ModifiedOn = DateTime.UtcNow;
        first.ModifiedBy = _currentUser.UserId;
        first.RecordVersion++;

        await _notificationService.SendAsync(
            first.ParticipantId, _currentUser.TenantId,
            NotificationType.SessionReminder,
            "You've been moved off the waitlist!",
            "A spot opened up and you are now registered for the session.",
            "Session", sessionId, cancellationToken);
    }

    private void EnsureCanManageSession(Session session)
    {
        if (_currentUser.IsAdminOrAbove || _currentUser.IsInRole(UserRole.KnowledgeTeam)) return;
        if (session.SpeakerId == _currentUser.UserId) return;
        throw new ForbiddenException("You do not have permission to manage this session.");
    }

    private static void ValidateMeetingLink(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new Domain.Exceptions.ValidationException(new Dictionary<string, string[]>
                { { "MeetingLink", new[] { "Meeting link must be a valid HTTPS URL." } } });

        if (!AllowedMeetingDomains.Any(d => uri.Host.EndsWith(d, StringComparison.OrdinalIgnoreCase)))
            throw new Domain.Exceptions.ValidationException(new Dictionary<string, string[]>
                { { "MeetingLink", new[] { "Meeting link must be from an approved platform (Teams, Zoom, Google Meet, Webex, GoTo, BlueJeans)." } } });
    }

    // B20: material URLs must be HTTPS (same standard as meeting links)
    private static void ValidateMaterialUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new Domain.Exceptions.ValidationException(new Dictionary<string, string[]>
                { { "Url", new[] { "Material URL must be a valid HTTPS URL." } } });
    }

    // B19: generic HTTPS-only URL validator used for RecordingUrl
    private static void ValidateHttpsUrl(string url, string fieldName)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new Domain.Exceptions.ValidationException(new Dictionary<string, string[]>
                { { fieldName, new[] { $"{fieldName} must be a valid HTTPS URL." } } });
    }

    private static SessionDto MapToDto(Session s) => new()
    {
        Id = s.Id,
        ProposalId = s.ProposalId,
        SpeakerId = s.SpeakerId,
        SpeakerName = s.Speaker != null ? s.Speaker.FullName : string.Empty,
        SpeakerPhotoUrl = s.Speaker?.ProfilePhotoUrl,
        Title = s.Title,
        CategoryId = s.CategoryId,
        CategoryName = s.Category != null ? s.Category.Name : string.Empty,
        Format = s.Format,
        DifficultyLevel = s.DifficultyLevel,
        ScheduledAt = s.ScheduledAt,
        DurationMinutes = s.DurationMinutes,
        MeetingLink = s.MeetingLink,
        MeetingPlatform = s.MeetingPlatform,
        ParticipantLimit = s.ParticipantLimit,
        RegisteredCount = s.Registrations != null ? s.Registrations.Count(r => r.Status == RegistrationStatus.Registered) : 0,
        Status = s.Status,
        IsPublic = s.IsPublic,
        RecordingUrl = s.RecordingUrl,
        Description = s.Description,
        Tags = s.SessionTags != null ? s.SessionTags.Where(t => t.Tag != null).Select(t => t.Tag.Name).ToList() : Array.Empty<string>(),
        RecordVersion = s.RecordVersion
    };
}
