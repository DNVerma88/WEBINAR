using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class UserLearningPathEnrollment : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LearningPathId { get; set; }
    public EnrolmentType EnrolmentType { get; set; }
    public decimal ProgressPercentage { get; set; }
    public int CompletedItemCount { get; set; }
    public DateTime? DeadlineAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public User? User { get; set; }
    public LearningPath? LearningPath { get; set; }
}
