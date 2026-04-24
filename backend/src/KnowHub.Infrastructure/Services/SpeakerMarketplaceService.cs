using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.SpeakerMarketplace;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class SpeakerMarketplaceService : ISpeakerMarketplaceService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly INotificationService _notificationService;

    public SpeakerMarketplaceService(
        KnowHubDbContext db,
        ICurrentUserAccessor currentUser,
        INotificationService notificationService)
    {
        _db = db;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<SpeakerAvailabilityDto> SetAvailabilityAsync(
        SetAvailabilityRequest request, CancellationToken cancellationToken)
    {
        if (request.AvailableTo <= request.AvailableFrom)
            throw new ValidationException("AvailableTo", "AvailableTo must be after AvailableFrom.");

        var availability = new SpeakerAvailability
        {
            TenantId = _currentUser.TenantId,
            UserId = _currentUser.UserId,
            AvailableFrom = request.AvailableFrom,
            AvailableTo = request.AvailableTo,
            IsRecurring = request.IsRecurring,
            RecurrencePattern = request.RecurrencePattern,
            Topics = request.Topics,
            Notes = request.Notes,
            IsBooked = false,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.SpeakerAvailability.Add(availability);
        await _db.SaveChangesAsync(cancellationToken);

        return await MapAvailabilityToDtoAsync(availability, cancellationToken);
    }

    public async Task<SpeakerAvailabilityDto> UpdateAvailabilityAsync(
        Guid availabilityId, UpdateAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var availability = await _db.SpeakerAvailability
            .FirstOrDefaultAsync(a => a.Id == availabilityId && a.TenantId == _currentUser.TenantId, cancellationToken);

        if (availability is null) throw new NotFoundException("SpeakerAvailability", availabilityId);

        if (availability.UserId != _currentUser.UserId && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("You can only update your own availability slots.");

        if (availability.IsBooked)
            throw new BusinessRuleException("Cannot update an availability slot that has been booked.");

        if (availability.RecordVersion != request.RecordVersion)
            throw new ConflictException("The record has been modified by another user. Please refresh and try again.");

        if (request.AvailableTo <= request.AvailableFrom)
            throw new ValidationException("AvailableTo", "AvailableTo must be after AvailableFrom.");

        availability.AvailableFrom = request.AvailableFrom;
        availability.AvailableTo = request.AvailableTo;
        availability.IsRecurring = request.IsRecurring;
        availability.RecurrencePattern = request.RecurrencePattern;
        availability.Topics = request.Topics;
        availability.Notes = request.Notes;
        availability.ModifiedBy = _currentUser.UserId;
        availability.ModifiedOn = DateTime.UtcNow;
        availability.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);

        return await MapAvailabilityToDtoAsync(availability, cancellationToken);
    }

    public async Task DeleteAvailabilityAsync(Guid availabilityId, CancellationToken cancellationToken)
    {
        var availability = await _db.SpeakerAvailability
            .FirstOrDefaultAsync(a => a.Id == availabilityId && a.TenantId == _currentUser.TenantId, cancellationToken);

        if (availability is null) throw new NotFoundException("SpeakerAvailability", availabilityId);

        if (availability.UserId != _currentUser.UserId && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("You can only delete your own availability slots.");

        if (availability.IsBooked)
            throw new BusinessRuleException("Cannot delete an availability slot that has been booked.");

        _db.SpeakerAvailability.Remove(availability);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<SpeakerAvailabilityDto>> GetAvailableSpeakersAsync(
        GetAvailableSpeakersRequest request, CancellationToken cancellationToken)
    {
        // Admins can see all available slots (including their own) to facilitate direct assignment
        var query = _db.SpeakerAvailability
            .Where(a => a.TenantId == _currentUser.TenantId && !a.IsBooked
                && (_currentUser.IsAdminOrAbove || a.UserId != _currentUser.UserId))
            .AsQueryable();

        if (request.FromDate.HasValue)
            query = query.Where(a => a.AvailableFrom >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.AvailableTo <= request.ToDate.Value);

        var total = await query.CountAsync(cancellationToken);

        var availabilities = await query
            .OrderBy(a => a.AvailableFrom)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .Join(_db.Users, a => a.UserId, u => u.Id, (a, u) => new
            {
                Availability = a,
                u.FullName,
                u.ProfilePhotoUrl,
                u.Department
            })
            .ToListAsync(cancellationToken);

        var dtos = availabilities
            .Where(x => request.TopicFilter is null ||
                x.Availability.Topics.Any(t =>
                    t.Contains(request.TopicFilter, StringComparison.OrdinalIgnoreCase)))
            .Select(x => new SpeakerAvailabilityDto(
                x.Availability.Id,
                x.Availability.UserId,
                x.FullName,
                x.ProfilePhotoUrl,
                x.Department,
                x.Availability.AvailableFrom,
                x.Availability.AvailableTo,
                x.Availability.IsRecurring,
                x.Availability.RecurrencePattern,
                x.Availability.Topics,
                x.Availability.Notes,
                x.Availability.IsBooked))
            .ToList();

        return new PagedResult<SpeakerAvailabilityDto> { Data = dtos, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<List<SpeakerAvailabilityDto>> GetMyAvailabilityAsync(CancellationToken cancellationToken)
    {
        var availabilities = await _db.SpeakerAvailability
            .Where(a => a.UserId == _currentUser.UserId && a.TenantId == _currentUser.TenantId)
            .OrderByDescending(a => a.AvailableFrom)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var tasks = availabilities.Select(a => MapAvailabilityToDtoAsync(a, cancellationToken));
        return (await Task.WhenAll(tasks)).ToList();
    }

    public async Task<SpeakerBookingDto> RequestBookingAsync(
        RequestBookingRequest request, CancellationToken cancellationToken)
    {
        var availability = await _db.SpeakerAvailability
            .FirstOrDefaultAsync(a => a.Id == request.SpeakerAvailabilityId
                && a.TenantId == _currentUser.TenantId, cancellationToken);

        if (availability is null) throw new NotFoundException("SpeakerAvailability", request.SpeakerAvailabilityId);

        if (availability.IsBooked)
            throw new BusinessRuleException("This availability slot is already booked.");

        if (availability.UserId == _currentUser.UserId)
            throw new BusinessRuleException("You cannot book your own availability slot.");

        var booking = new SpeakerBooking
        {
            TenantId = _currentUser.TenantId,
            SpeakerAvailabilityId = request.SpeakerAvailabilityId,
            SpeakerUserId = availability.UserId,
            RequesterUserId = _currentUser.UserId,
            Topic = request.Topic,
            Description = request.Description,
            Status = BookingStatus.Pending,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.SpeakerBookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            availability.UserId, _currentUser.TenantId,
            NotificationType.General,
            "New Speaker Booking Request",
            $"You have a new booking request for \"{request.Topic}\".",
            "SpeakerBooking", booking.Id, cancellationToken);

        return await MapBookingToDtoAsync(booking, cancellationToken);
    }

    public async Task<PagedResult<SpeakerBookingDto>> GetMyBookingsAsync(
        GetMyBookingsRequest request, CancellationToken cancellationToken)
    {
        var query = _db.SpeakerBookings
            .Where(b => b.TenantId == _currentUser.TenantId
                && (b.SpeakerUserId == _currentUser.UserId || b.RequesterUserId == _currentUser.UserId))
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(b => b.Status == request.Status.Value);

        if (request.AsSpeaker.HasValue)
            query = request.AsSpeaker.Value
                ? query.Where(b => b.SpeakerUserId == _currentUser.UserId)
                : query.Where(b => b.RequesterUserId == _currentUser.UserId);

        var total = await query.CountAsync(cancellationToken);

        var bookings = await query
            .OrderByDescending(b => b.CreatedDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var userIds = bookings.SelectMany(b => new[] { b.SpeakerUserId, b.RequesterUserId }).Distinct().ToList();
        var userNames = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var dtos = bookings.Select(b => new SpeakerBookingDto(
            b.Id,
            b.SpeakerUserId,
            userNames.GetValueOrDefault(b.SpeakerUserId, "Unknown"),
            b.RequesterUserId,
            userNames.GetValueOrDefault(b.RequesterUserId, "Unknown"),
            b.Topic, b.Description, b.Status, b.CreatedDate,
            b.RespondedAt, b.ResponseNotes, b.LinkedSessionId)).ToList();

        return new PagedResult<SpeakerBookingDto> { Data = dtos, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }

    public async Task<SpeakerBookingDto> RespondToBookingAsync(
        Guid bookingId, RespondToBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await _db.SpeakerBookings
            .Include(b => b.SpeakerAvailability)
            .FirstOrDefaultAsync(b => b.Id == bookingId
                && b.TenantId == _currentUser.TenantId
                && (b.SpeakerUserId == _currentUser.UserId || _currentUser.IsAdminOrAbove),
                cancellationToken);

        if (booking is null) throw new NotFoundException("SpeakerBooking", bookingId);
        if (booking.Status != BookingStatus.Pending)
            throw new BusinessRuleException("Only pending bookings can be responded to.");

        var decision = request.IsAccepted ? BookingStatus.Accepted : BookingStatus.Declined;
        booking.Status = decision;
        booking.RespondedAt = DateTime.UtcNow;
        booking.ResponseNotes = request.ResponseNotes;
        booking.ModifiedBy = _currentUser.UserId;
        booking.ModifiedOn = DateTime.UtcNow;
        booking.RecordVersion++;

        if (request.IsAccepted && booking.SpeakerAvailability is not null)
        {
            booking.SpeakerAvailability.IsBooked = true;
            booking.SpeakerAvailability.ModifiedBy = _currentUser.UserId;
            booking.SpeakerAvailability.ModifiedOn = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var decisionText = request.IsAccepted ? "accepted" : "declined";
        await _notificationService.SendAsync(
            booking.RequesterUserId, _currentUser.TenantId,
            NotificationType.General,
            "Booking Request Update",
            $"Your booking request for \"{booking.Topic}\" has been {decisionText}.",
            "SpeakerBooking", booking.Id, cancellationToken);

        return await MapBookingToDtoAsync(booking, cancellationToken);
    }

    public async Task<SpeakerBookingDto> AdminAssignAsync(
        AdminAssignRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove && !_currentUser.IsInRole(UserRole.KnowledgeTeam))
            throw new ForbiddenException("Only KnowledgeTeam and Admins can directly assign speakers.");

        var availability = await _db.SpeakerAvailability
            .FirstOrDefaultAsync(a => a.Id == request.SpeakerAvailabilityId
                && a.TenantId == _currentUser.TenantId, cancellationToken);

        if (availability is null) throw new NotFoundException("SpeakerAvailability", request.SpeakerAvailabilityId);
        if (availability.IsBooked) throw new BusinessRuleException("This availability slot is already booked.");

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.TenantId == _currentUser.TenantId, cancellationToken);

        if (session is null) throw new NotFoundException("Session", request.SessionId);

        // Create booking already in Accepted state — no request/approval cycle needed
        var booking = new SpeakerBooking
        {
            TenantId = _currentUser.TenantId,
            SpeakerAvailabilityId = request.SpeakerAvailabilityId,
            SpeakerUserId = availability.UserId,
            RequesterUserId = _currentUser.UserId,
            Topic = request.Topic,
            Description = request.Description,
            Status = BookingStatus.Accepted,
            RespondedAt = DateTime.UtcNow,
            LinkedSessionId = request.SessionId,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        availability.IsBooked = true;
        availability.ModifiedBy = _currentUser.UserId;
        availability.ModifiedOn = DateTime.UtcNow;

        session.SpeakerId = availability.UserId;
        session.ModifiedBy = _currentUser.UserId;
        session.ModifiedOn = DateTime.UtcNow;
        session.RecordVersion++;

        _db.SpeakerBookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            availability.UserId, _currentUser.TenantId,
            NotificationType.General,
            "You've Been Assigned as a Session Speaker",
            $"An admin has directly assigned you as the speaker for session \"{session.Title}\" based on your availability listing for \"{request.Topic}\".",
            "SpeakerBooking", booking.Id, cancellationToken);

        return await MapBookingToDtoAsync(booking, cancellationToken);
    }

    public async Task<SpeakerBookingDto> CompleteBookingAsync(
        Guid bookingId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only administrators can mark bookings as complete.");

        var booking = await _db.SpeakerBookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.TenantId == _currentUser.TenantId, cancellationToken);

        if (booking is null) throw new NotFoundException("SpeakerBooking", bookingId);
        if (booking.Status != BookingStatus.Accepted)
            throw new BusinessRuleException("Only accepted bookings can be marked as complete.");

        booking.Status = BookingStatus.Completed;
        booking.ModifiedBy = _currentUser.UserId;
        booking.ModifiedOn = DateTime.UtcNow;
        booking.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);

        return await MapBookingToDtoAsync(booking, cancellationToken);
    }

    public async Task<SpeakerBookingDto> LinkToSessionAsync(
        Guid bookingId, Guid sessionId, CancellationToken cancellationToken)
    {
        var booking = await _db.SpeakerBookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.TenantId == _currentUser.TenantId, cancellationToken);

        if (booking is null) throw new NotFoundException("SpeakerBooking", bookingId);

        var isAdminOrKt = _currentUser.IsAdminOrAbove || _currentUser.IsInRole(UserRole.KnowledgeTeam);
        if (!isAdminOrKt && booking.RequesterUserId != _currentUser.UserId)
            throw new ForbiddenException("Only the requester, KnowledgeTeam, or an Admin can link this booking to a session.");

        if (booking.Status != BookingStatus.Accepted)
            throw new BusinessRuleException("Only accepted bookings can be linked to a session.");

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);

        if (session is null) throw new NotFoundException("Session", sessionId);

        booking.LinkedSessionId = sessionId;
        booking.ModifiedBy = _currentUser.UserId;
        booking.ModifiedOn = DateTime.UtcNow;
        booking.RecordVersion++;

        session.SpeakerId = booking.SpeakerUserId;
        session.ModifiedBy = _currentUser.UserId;
        session.ModifiedOn = DateTime.UtcNow;
        session.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.SendAsync(
            booking.SpeakerUserId, _currentUser.TenantId,
            NotificationType.General,
            "You've Been Assigned as a Session Speaker",
            $"Your booking for \"{booking.Topic}\" has been linked to a scheduled session.",
            "SpeakerBooking", booking.Id, cancellationToken);

        return await MapBookingToDtoAsync(booking, cancellationToken);
    }

    private async Task<SpeakerAvailabilityDto> MapAvailabilityToDtoAsync(
        SpeakerAvailability availability, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Where(u => u.Id == availability.UserId)
            .AsNoTracking()
            .Select(u => new { u.FullName, u.ProfilePhotoUrl, u.Department })
            .FirstOrDefaultAsync(cancellationToken);

        return new SpeakerAvailabilityDto(
            availability.Id, availability.UserId,
            user?.FullName ?? "Unknown",
            user?.ProfilePhotoUrl,
            user?.Department,
            availability.AvailableFrom, availability.AvailableTo,
            availability.IsRecurring, availability.RecurrencePattern,
            availability.Topics, availability.Notes, availability.IsBooked);
    }

    private async Task<SpeakerBookingDto> MapBookingToDtoAsync(
        SpeakerBooking booking, CancellationToken cancellationToken)
    {
        var userIds = new[] { booking.SpeakerUserId, booking.RequesterUserId }.Distinct().ToList();
        var names = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        return new SpeakerBookingDto(
            booking.Id, booking.SpeakerUserId,
            names.GetValueOrDefault(booking.SpeakerUserId, "Unknown"),
            booking.RequesterUserId,
            names.GetValueOrDefault(booking.RequesterUserId, "Unknown"),
            booking.Topic, booking.Description, booking.Status, booking.CreatedDate,
            booking.RespondedAt, booking.ResponseNotes, booking.LinkedSessionId);
    }
}
