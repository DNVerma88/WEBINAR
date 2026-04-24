namespace KnowHub.Domain.Entities;

public class ContributorProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string? AreasOfExpertise { get; set; }
    public string? TechnologiesKnown { get; set; }
    public string? Bio { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalSessionsDelivered { get; set; }
    public int FollowerCount { get; set; }
    public decimal EndorsementScore { get; set; }
    public bool IsKnowledgeBroker { get; set; }
    public bool AvailableForMentoring { get; set; }

    public User User { get; set; } = null!;
}
