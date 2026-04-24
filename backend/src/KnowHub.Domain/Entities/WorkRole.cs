namespace KnowHub.Domain.Entities;

public class WorkRole : BaseEntity
{
    public string Code         { get; set; } = string.Empty;
    public string Name         { get; set; } = string.Empty;
    public string Category     { get; set; } = string.Empty;
    public int    DisplayOrder { get; set; }
    public bool   IsActive     { get; set; } = true;

    // Navigation
    public ICollection<AssessmentGroupMember> GroupMembers { get; set; } = new List<AssessmentGroupMember>();
}
