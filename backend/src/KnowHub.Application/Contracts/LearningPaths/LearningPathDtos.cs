using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class LearningPathDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Objective { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public DifficultyLevel DifficultyLevel { get; init; }
    public int EstimatedDurationMinutes { get; init; }
    public bool IsPublished { get; init; }
    public bool IsAssignable { get; init; }
    public string? CoverImageUrl { get; init; }
    public int ItemCount { get; init; }
}

public class LearningPathItemDto
{
    public Guid Id { get; init; }
    public LearningPathItemType ItemType { get; init; }
    public Guid? SessionId { get; init; }
    public string? SessionTitle { get; init; }
    public Guid? KnowledgeAssetId { get; init; }
    public string? AssetTitle { get; init; }
    public int OrderSequence { get; init; }
    public bool IsRequired { get; init; }
}

public class LearningPathDetailDto : LearningPathDto
{
    public List<LearningPathItemDto> Items { get; init; } = new();
    public int EnrolledCount { get; init; }
}

public class EnrolmentProgressDto
{
    public Guid UserId { get; init; }
    public Guid LearningPathId { get; init; }
    public decimal ProgressPercentage { get; init; }
    public int CompletedItemCount { get; init; }
    public int TotalItemCount { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public class LearningPathCertificateDto
{
    public Guid Id { get; init; }
    public string CertificateNumber { get; init; } = string.Empty;
    public string CertificateUrl { get; init; } = string.Empty;
    public DateTime IssuedAt { get; init; }
    public string PathTitle { get; init; } = string.Empty;
}

public class UserEnrolmentDto
{
    public Guid EnrolmentId { get; init; }
    public Guid LearningPathId { get; init; }
    public string PathTitle { get; init; } = string.Empty;
    public decimal ProgressPercentage { get; init; }
    public EnrolmentType EnrolmentType { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? DeadlineAt { get; init; }
}

public class CreateLearningPathRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Objective { get; set; }
    public Guid? CategoryId { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string? CoverImageUrl { get; set; }
}

public class UpdateLearningPathRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Objective { get; set; }
    public Guid? CategoryId { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsPublished { get; set; }
}

public class GetLearningPathsRequest
{
    public Guid? CategoryId { get; set; }
    public DifficultyLevel? DifficultyLevel { get; set; }
    public string? SearchTerm { get; set; }
    public bool? IsPublished { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AddLearningPathItemRequest
{
    public LearningPathItemType ItemType { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? KnowledgeAssetId { get; set; }
    public bool IsRequired { get; set; } = true;
}
