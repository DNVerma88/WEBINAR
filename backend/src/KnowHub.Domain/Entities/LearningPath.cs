using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class LearningPath : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Objective { get; set; }
    public Guid? CategoryId { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public bool IsPublished { get; set; }
    public bool IsAssignable { get; set; } = true;
    public string? CoverImageUrl { get; set; }

    public Category? Category { get; set; }
    public ICollection<LearningPathItem> Items { get; set; } = new List<LearningPathItem>();
    public ICollection<UserLearningPathEnrollment> Enrollments { get; set; } = new List<UserLearningPathEnrollment>();
    public ICollection<LearningPathCertificate> Certificates { get; set; } = new List<LearningPathCertificate>();
    public ICollection<LearningPathCohort> Cohorts { get; set; } = new List<LearningPathCohort>();
}
