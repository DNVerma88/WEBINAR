namespace KnowHub.Application.Contracts;

public class ContributorProfileDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string UserFullName { get; init; } = string.Empty;
    public string? ProfilePhotoUrl { get; init; }
    public string? AreasOfExpertise { get; init; }
    public string? TechnologiesKnown { get; init; }
    public string? Bio { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalSessionsDelivered { get; init; }
    public int FollowerCount { get; init; }
    public decimal EndorsementScore { get; init; }
    public bool IsKnowledgeBroker { get; init; }
    public bool AvailableForMentoring { get; init; }
    public int RecordVersion { get; init; }
}

public class UpdateContributorProfileRequest
{
    public string? AreasOfExpertise { get; set; }
    public string? TechnologiesKnown { get; set; }
    public string? Bio { get; set; }
    public bool AvailableForMentoring { get; set; }
    public int RecordVersion { get; set; }
}
