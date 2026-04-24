namespace KnowHub.Domain.Entities;

public class AssessmentGroupCoLead : BaseEntity
{
    public Guid      GroupId       { get; set; }
    public Guid      UserId        { get; set; }
    public DateTime  EffectiveFrom { get; set; }
    public DateTime? EffectiveTo   { get; set; }
    public bool      IsActive      { get; set; } = true;

    // Navigation
    public AssessmentGroup Group { get; set; } = null!;
    public User             User  { get; set; } = null!;
}
