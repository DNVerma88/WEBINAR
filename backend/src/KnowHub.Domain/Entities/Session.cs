using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class Session : BaseEntity
{
    public Guid ProposalId { get; set; }
    public Guid SpeakerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public SessionFormat Format { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string MeetingLink { get; set; } = string.Empty;
    public MeetingPlatform MeetingPlatform { get; set; }
    public int? ParticipantLimit { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public bool IsPublic { get; set; } = true;
    public string? RecordingUrl { get; set; }
    public string? Description { get; set; }

    public SessionProposal Proposal { get; set; } = null!;
    public User Speaker { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<SessionTag> SessionTags { get; set; } = new List<SessionTag>();
    public ICollection<SessionMaterial> Materials { get; set; } = new List<SessionMaterial>();
    public ICollection<SessionRegistration> Registrations { get; set; } = new List<SessionRegistration>();
    public ICollection<KnowledgeAsset> KnowledgeAssets { get; set; } = new List<KnowledgeAsset>();
    public ICollection<SessionRating> Ratings { get; set; } = new List<SessionRating>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
