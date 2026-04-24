using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities.Talent;

public class ScreeningCandidate
{
    public Guid Id { get; set; }
    public Guid ScreeningJobId { get; set; }
    public string FileName { get; set; } = "";
    public string StorageProviderType { get; set; } = "Local";
    public string FileReference { get; set; } = "";
    public CandidateStatus Status { get; set; } = CandidateStatus.Queued;
    public string? ErrorMessage { get; set; }
    public string? ExtractedText { get; set; }
    public string? CandidateName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal? SemanticSimilarityScore { get; set; }
    public decimal? SkillsDepthScore { get; set; }
    public decimal? LegitimacyScore { get; set; }
    public decimal? OverallScore { get; set; }
    public string? Recommendation { get; set; }
    public string? ScoreSummary { get; set; }
    public string? SkillsMatched { get; set; }  // JSON array
    public string? SkillsGap { get; set; }       // JSON array
    public string? RedFlags { get; set; }         // JSON array
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ScoredAt { get; set; }
    public ScreeningJob ScreeningJob { get; set; } = null!;
}
