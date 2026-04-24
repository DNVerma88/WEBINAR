using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities.Talent;

public class ScreeningJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string JobTitle { get; set; } = "";
    public string? JdText { get; set; }
    public string? JdFileReference { get; set; }  // JSON StorageFileReference
    public string? JdEmbedding { get; set; }       // JSON float[] cached embedding
    public string? PromptTemplate { get; set; }       // AI scoring prompt; null → uses ResumeScorer.DefaultPromptTemplate
    public ScreeningJobStatus Status { get; set; } = ScreeningJobStatus.Pending;
    public int TotalCandidates { get; set; }
    public int ProcessedCandidates { get; set; }
    public int ProgressPercent { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public ICollection<ScreeningCandidate> Candidates { get; set; } = [];
}
