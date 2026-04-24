namespace KnowHub.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UsageCount { get; set; }
    public int PostCount { get; set; }
    public bool IsOfficial { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SessionTag> SessionTags { get; set; } = new List<SessionTag>();
    public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
    public ICollection<CommunityPostTag> CommunityPostTags { get; set; } = new List<CommunityPostTag>();
}
