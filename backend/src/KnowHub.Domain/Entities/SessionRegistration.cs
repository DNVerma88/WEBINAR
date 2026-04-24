using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class SessionRegistration : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid ParticipantId { get; set; }
    public int? WaitlistPosition { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? AttendedAt { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Registered;

    public Session Session { get; set; } = null!;
    public User Participant { get; set; } = null!;
}
