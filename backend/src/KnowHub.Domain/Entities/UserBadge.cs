namespace KnowHub.Domain.Entities;

public class UserBadge : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid BadgeId { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
    public string? AwardReason { get; set; }
    public int XpGranted { get; set; }

    public User User { get; set; } = null!;
    public Badge Badge { get; set; } = null!;
}
