using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class LearningPathCohortDto
{
    public Guid Id { get; init; }
    public Guid LearningPathId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int? MaxParticipants { get; init; }
    public CohortStatus Status { get; init; }
    public DateTime CreatedDate { get; init; }
}

public class CreateLearningPathCohortRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxParticipants { get; set; }
}

public class UpdateLearningPathCohortRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxParticipants { get; set; }
    public CohortStatus Status { get; set; }
}

public interface ILearningPathCohortService
{
    Task<List<LearningPathCohortDto>> GetCohortsAsync(Guid learningPathId, CancellationToken cancellationToken);
    Task<LearningPathCohortDto> CreateCohortAsync(Guid learningPathId, CreateLearningPathCohortRequest request, CancellationToken cancellationToken);
    Task<LearningPathCohortDto> UpdateCohortAsync(Guid learningPathId, Guid cohortId, UpdateLearningPathCohortRequest request, CancellationToken cancellationToken);
    Task DeleteCohortAsync(Guid learningPathId, Guid cohortId, CancellationToken cancellationToken);
}
