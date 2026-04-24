using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class UserXpEvent : BaseEntity
{
    public Guid UserId { get; set; }
    public XpEventType EventType { get; set; }
    public int XpAmount { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
