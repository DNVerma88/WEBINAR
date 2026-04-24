using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class LearningPathCohort : BaseEntity
{
    public Guid LearningPathId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxParticipants { get; set; }
    public CohortStatus Status { get; set; } = CohortStatus.Scheduled;

    public LearningPath? LearningPath { get; set; }
}
