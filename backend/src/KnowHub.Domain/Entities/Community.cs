namespace KnowHub.Domain.Entities;

public class Community : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public string? CoverImageUrl { get; set; }
    public int MemberCount { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CommunityMember> Members { get; set; } = new List<CommunityMember>();
}
