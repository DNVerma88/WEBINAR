using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class SessionDto
{
    public Guid Id { get; init; }
    public Guid ProposalId { get; init; }
    public Guid SpeakerId { get; init; }
    public string SpeakerName { get; init; } = string.Empty;
    public string? SpeakerPhotoUrl { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public SessionFormat Format { get; init; }
    public DifficultyLevel DifficultyLevel { get; init; }
    public DateTime ScheduledAt { get; init; }
    public int DurationMinutes { get; init; }
    public string MeetingLink { get; init; } = string.Empty;
    public MeetingPlatform MeetingPlatform { get; init; }
    public int? ParticipantLimit { get; init; }
    public int RegisteredCount { get; init; }
    public SessionStatus Status { get; init; }
    public bool IsPublic { get; init; }
    public string? RecordingUrl { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public int RecordVersion { get; init; }
}

public class CreateSessionRequest
{
    public Guid ProposalId { get; set; }
    public Guid? SpeakerId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string MeetingLink { get; set; } = string.Empty;
    public MeetingPlatform MeetingPlatform { get; set; }
    public int? ParticipantLimit { get; set; }
    public bool IsPublic { get; set; } = true;
    public List<Guid> TagIds { get; set; } = new();
}

public class UpdateSessionRequest
{
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string MeetingLink { get; set; } = string.Empty;
    public MeetingPlatform MeetingPlatform { get; set; }
    public int? ParticipantLimit { get; set; }
    public bool IsPublic { get; set; }
    public string? RecordingUrl { get; set; }
    public Guid? SpeakerId { get; set; }
    public List<Guid> TagIds { get; set; } = new();
    public int RecordVersion { get; set; }
}

public class GetSessionsRequest
{
    public Guid? CategoryId { get; set; }
    public Guid? TagId { get; set; }
    public DifficultyLevel? DifficultyLevel { get; set; }
    public SessionFormat? Format { get; set; }
    public SessionStatus? Status { get; set; }
    public Guid? SpeakerId { get; set; }
    public string? Department { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
