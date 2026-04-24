namespace KnowHub.Domain.Entities;

public class UserSuspension : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid SuspendedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime SuspendedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? LiftedByUserId { get; set; }
    public DateTime? LiftedAt { get; set; }
    public string? LiftReason { get; set; }

    public User? User { get; set; }
    public User? SuspendedBy { get; set; }
    public User? LiftedBy { get; set; }
}
