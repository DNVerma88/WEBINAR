using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class SpeakerBooking : BaseEntity
{
    public Guid SpeakerAvailabilityId { get; set; }
    public Guid SpeakerUserId { get; set; }
    public Guid RequesterUserId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime? RespondedAt { get; set; }
    public string? ResponseNotes { get; set; }
    public Guid? LinkedSessionId { get; set; }

    public SpeakerAvailability? SpeakerAvailability { get; set; }
    public User? Speaker { get; set; }
    public User? Requester { get; set; }
    public Session? LinkedSession { get; set; }
}
