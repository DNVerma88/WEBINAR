namespace KnowHub.Domain.Entities;

public class AssessmentGroup : BaseEntity
{
    public string  GroupName          { get; set; } = string.Empty;
    public string  GroupCode          { get; set; } = string.Empty;
    public string? Description        { get; set; }
    public Guid    PrimaryLeadUserId  { get; set; }
    public string? AssessmentCategory { get; set; }
    public bool    IsActive           { get; set; } = true;

    // Navigation
    public User                                 PrimaryLead  { get; set; } = null!;
    public ICollection<AssessmentGroupMember>   GroupMembers { get; set; } = new List<AssessmentGroupMember>();
    public ICollection<AssessmentGroupCoLead>   GroupCoLeads { get; set; } = new List<AssessmentGroupCoLead>();
    public ICollection<EmployeeAssessment>       Assessments  { get; set; } = new List<EmployeeAssessment>();
}
