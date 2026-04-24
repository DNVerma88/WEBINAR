using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class Badge : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? Criteria { get; set; }
    public string BadgeCategory { get; set; } = string.Empty;
    public int XpGranted { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
