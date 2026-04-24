using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class SurveyInvitation : BaseEntity
{
    public Guid SurveyId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>
    /// SHA-256 hex digest of the one-time token (lowercase hex, 64 chars).
    /// The plaintext token is NEVER stored — it is generated, emailed, and discarded.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;
    public SurveyInvitationStatus Status { get; set; } = SurveyInvitationStatus.Pending;
    public DateTime? SentAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? TokenAccessedAt { get; set; }
    public int ResendCount { get; set; } = 0;

    // Navigation
    public Survey Survey { get; set; } = null!;
    public User User { get; set; } = null!;
    public SurveyResponse? Response { get; set; }
}
