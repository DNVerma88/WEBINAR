namespace KnowHub.Domain.Entities;

public class RubricDefinition : BaseEntity
{
    public string    DesignationCode     { get; set; } = string.Empty;
    public Guid      RatingScaleId       { get; set; }
    public string    BehaviorDescription { get; set; } = string.Empty;
    public string    ProcessDescription  { get; set; } = string.Empty;
    public string    EvidenceDescription { get; set; } = string.Empty;
    public int       VersionNo           { get; set; } = 1;
    public DateOnly  EffectiveFrom       { get; set; }
    public DateOnly? EffectiveTo         { get; set; }
    public bool      IsActive            { get; set; } = true;

    // Navigation
    public RatingScale RatingScale { get; set; } = null!;
}
