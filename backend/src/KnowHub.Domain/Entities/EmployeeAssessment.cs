using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class EmployeeAssessment : BaseEntity
{
    public Guid             UserId               { get; set; }
    public Guid             GroupId              { get; set; }
    public Guid             AssessmentPeriodId   { get; set; }
    public string           RoleCode             { get; set; } = string.Empty;
    public string?          Designation          { get; set; }
    public Guid             RatingScaleId        { get; set; }
    public int              RatingValue          { get; set; }
    public string?          Comment              { get; set; }
    public string?          EvidenceNotes        { get; set; }
    public string?          ParameterSummaryJson { get; set; }
    public AssessmentStatus Status               { get; set; } = AssessmentStatus.Draft;
    public Guid?            SubmittedBy          { get; set; }
    public DateTime?        SubmittedOn          { get; set; }

    // Navigation
    public User                                             Employee         { get; set; } = null!;
    public AssessmentGroup                                 Group            { get; set; } = null!;
    public AssessmentPeriod                                Period           { get; set; } = null!;
    public RatingScale                                     RatingScale      { get; set; } = null!;
    public User?                                           Submitter        { get; set; }
    public ICollection<EmployeeAssessmentParameterDetail>  ParameterDetails { get; set; } = new List<EmployeeAssessmentParameterDetail>();
    public ICollection<AssessmentAuditLog>                 AuditLogs        { get; set; } = new List<AssessmentAuditLog>();
}
