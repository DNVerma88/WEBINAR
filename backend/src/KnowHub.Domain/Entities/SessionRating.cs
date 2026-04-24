namespace KnowHub.Domain.Entities;

public class SessionRating : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid RaterId { get; set; }
    public int SessionScore { get; set; }
    public int SpeakerScore { get; set; }
    public string? FeedbackText { get; set; }
    public string? NextSessionSuggestion { get; set; }

    public Session Session { get; set; } = null!;
    public User Rater { get; set; } = null!;
}
