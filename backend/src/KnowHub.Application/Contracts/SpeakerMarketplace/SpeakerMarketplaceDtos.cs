using KnowHub.Application.Models;
using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts.SpeakerMarketplace;

public record SetAvailabilityRequest(
    DateTime AvailableFrom,
    DateTime AvailableTo,
    bool IsRecurring,
    string? RecurrencePattern,
    List<string> Topics,
    string? Notes
);

public record UpdateAvailabilityRequest(
    DateTime AvailableFrom,
    DateTime AvailableTo,
    bool IsRecurring,
    string? RecurrencePattern,
    List<string> Topics,
    string? Notes,
    int RecordVersion
);

public record GetAvailableSpeakersRequest(
    string? TopicFilter,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber = 1,
    int PageSize = 20
);

public record RequestBookingRequest(
    Guid SpeakerAvailabilityId,
    string Topic,
    string? Description
);

public record RespondToBookingRequest(
    bool IsAccepted,
    string? ResponseNotes
);

public record GetMyBookingsRequest(
    BookingStatus? Status,
    bool? AsSpeaker = null,
    int PageNumber = 1,
    int PageSize = 20
);

public record AdminAssignRequest(
    Guid SpeakerAvailabilityId,
    Guid SessionId,
    string Topic,
    string? Description
);

public record LinkBookingToSessionRequest(Guid SessionId);

public record SpeakerAvailabilityDto(
    Guid Id,
    Guid UserId,
    string SpeakerName,
    string? SpeakerAvatarUrl,
    string? SpeakerDepartment,
    DateTime AvailableFrom,
    DateTime AvailableTo,
    bool IsRecurring,
    string? RecurrencePattern,
    List<string> Topics,
    string? Notes,
    bool IsBooked
);

public record SpeakerBookingDto(
    Guid Id,
    Guid SpeakerUserId,
    string SpeakerName,
    Guid RequesterUserId,
    string RequesterName,
    string Topic,
    string? Description,
    BookingStatus Status,
    DateTime CreatedDate,
    DateTime? RespondedAt,
    string? ResponseNotes,
    Guid? LinkedSessionId
);
