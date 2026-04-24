using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class AssessmentAuditLog : BaseEntity
{
    public Guid?                EmployeeAssessmentId { get; set; }
    public string               RelatedEntityType    { get; set; } = string.Empty;
    public Guid                 RelatedEntityId      { get; set; }
    public AssessmentActionType ActionType           { get; set; }
    public string?              OldValueJson         { get; set; }
    public string?              NewValueJson         { get; set; }
    public Guid                 ChangedBy            { get; set; }
    public DateTime             ChangedOn            { get; set; }
    public string?              Remarks              { get; set; }

    // Navigation
    public EmployeeAssessment? Assessment    { get; set; }
    public User                ChangedByUser { get; set; } = null!;
}
