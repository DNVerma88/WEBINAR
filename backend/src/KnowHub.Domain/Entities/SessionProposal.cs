using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class SessionProposal : BaseEntity
{
    public Guid ProposerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string? DepartmentRelevance { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ProblemStatement { get; set; }
    public string? LearningOutcomes { get; set; }
    public string? TargetAudience { get; set; }
    public SessionFormat Format { get; set; }
    public int Duration { get; set; }
    public DateTime? PreferredDate { get; set; }
    public TimeOnly? PreferredTime { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; }
    public string? RelatedProject { get; set; }
    public bool AllowRecording { get; set; } = true;
    public ProposalStatus Status { get; set; } = ProposalStatus.Draft;
    public DateTime? SubmittedAt { get; set; }

    public User Proposer { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ProposalApproval> Approvals { get; set; } = new List<ProposalApproval>();
    public ICollection<SessionMaterial> Materials { get; set; } = new List<SessionMaterial>();
    public Session? Session { get; set; }
}
