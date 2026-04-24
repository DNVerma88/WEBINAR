using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class AssessmentPeriod : BaseEntity
{
    public string                    Name       { get; set; } = string.Empty;
    public AssessmentPeriodFrequency Frequency  { get; set; }
    public DateOnly                  StartDate  { get; set; }
    public DateOnly                  EndDate    { get; set; }
    public int                       Year       { get; set; }
    public int?                      WeekNumber { get; set; }
    public AssessmentPeriodStatus    Status     { get; set; } = AssessmentPeriodStatus.Draft;
    public bool                      IsActive   { get; set; } = true;

    // Navigation
    public ICollection<EmployeeAssessment> Assessments { get; set; } = new List<EmployeeAssessment>();
}
