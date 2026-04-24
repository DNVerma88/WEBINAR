using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class SessionProposalDto
{
    public Guid Id { get; init; }
    public Guid ProposerId { get; init; }
    public string ProposerName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
    public string? DepartmentRelevance { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Prerequisites { get; init; }
    public string? ExpectedOutcomes { get; init; }
    public string? TargetAudience { get; init; }
    public SessionFormat Format { get; init; }
    public int EstimatedDurationMinutes { get; init; }
    public DateTime? PreferredDate { get; init; }
    public DifficultyLevel DifficultyLevel { get; init; }
    public string? RelatedProject { get; init; }
    public bool AllowRecording { get; init; }
    public ProposalStatus Status { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime CreatedDate { get; init; }
    public int RecordVersion { get; init; }
}

public class CreateSessionProposalRequest
{
    public string Title { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string? DepartmentRelevance { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Prerequisites { get; set; }
    public string? ExpectedOutcomes { get; set; }
    public string? TargetAudience { get; set; }
    public SessionFormat Format { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public DateTime? PreferredDate { get; set; }
    public TimeOnly? PreferredTime { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; }
    public string? RelatedProject { get; set; }
    public bool AllowRecording { get; set; } = true;
}

public class UpdateSessionProposalRequest
{
    public string Title { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string? DepartmentRelevance { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Prerequisites { get; set; }
    public string? ExpectedOutcomes { get; set; }
    public string? TargetAudience { get; set; }
    public SessionFormat Format { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public DateTime? PreferredDate { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; }
    public string? RelatedProject { get; set; }
    public bool AllowRecording { get; set; }
    public int RecordVersion { get; set; }
}

public class GetSessionProposalsRequest
{
    public ProposalStatus? Status { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ProposerId { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ApproveProposalRequest
{
    public string? Comment { get; set; }
}

public class RejectProposalRequest
{
    public string Comment { get; set; } = string.Empty;
}

public class RequestRevisionRequest
{
    public string Comment { get; set; } = string.Empty;
}
