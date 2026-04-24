namespace KnowHub.Domain.Entities;

public class EmployeeAssessmentParameterDetail : BaseEntity
{
    public Guid    EmployeeAssessmentId   { get; set; }
    public Guid    ParameterId            { get; set; }
    public Guid    ParameterRatingScaleId { get; set; }
    public string? Comment               { get; set; }
    public string? EvidenceNotes         { get; set; }

    // Navigation
    public EmployeeAssessment Assessment      { get; set; } = null!;
    public ParameterMaster    Parameter       { get; set; } = null!;
    public RatingScale        ParameterRating { get; set; } = null!;
}
