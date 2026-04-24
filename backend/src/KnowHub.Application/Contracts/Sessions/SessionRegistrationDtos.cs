using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class SessionRegistrationDto
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public Guid ParticipantId { get; init; }
    public string ParticipantName { get; init; } = string.Empty;
    public int? WaitlistPosition { get; init; }
    public DateTime RegisteredAt { get; init; }
    public RegistrationStatus Status { get; init; }
}

public class SessionRatingDto
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public Guid RaterId { get; init; }
    public string RaterName { get; init; } = string.Empty;
    public int SessionScore { get; init; }
    public int SpeakerScore { get; init; }
    public string? FeedbackText { get; init; }
    public string? NextSessionSuggestion { get; init; }
    public DateTime CreatedDate { get; init; }
}

public class SessionRatingSummaryDto
{
    public double AverageSessionScore { get; init; }
    public double AverageSpeakerScore { get; init; }
    public int TotalRatings { get; init; }
}

public class SubmitSessionRatingRequest
{
    public int SessionScore { get; set; }
    public int SpeakerScore { get; set; }
    public string? FeedbackText { get; set; }
    public string? NextSessionSuggestion { get; set; }
}
