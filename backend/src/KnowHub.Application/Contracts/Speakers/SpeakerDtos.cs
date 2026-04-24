namespace KnowHub.Application.Contracts;

public class SpeakerDto
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? ProfilePhotoUrl { get; init; }
    public string? Department { get; init; }
    public string? Designation { get; init; }
    public string? AreasOfExpertise { get; init; }
    public string? TechnologiesKnown { get; init; }
    public string? Bio { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalSessionsDelivered { get; init; }
    public int FollowerCount { get; init; }
    public bool IsKnowledgeBroker { get; init; }
    public bool IsFollowedByCurrentUser { get; init; }
    public bool AvailableForMentoring { get; init; }
}

public class SpeakerDetailDto : SpeakerDto
{
    public List<SpeakerSessionDto> RecentSessions { get; init; } = new();
}

public class SpeakerSessionDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime ScheduledAt { get; init; }
    public string? CategoryName { get; init; }
    public int DurationMinutes { get; init; }
    public decimal AverageRating { get; init; }
}

public class GetSpeakersRequest
{
    public string? SearchTerm { get; set; }
    public string? ExpertiseArea { get; set; }
    public string? Technology { get; set; }
    public Guid? CategoryId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
