using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class UserXpDto
{
    public Guid UserId { get; init; }
    public int TotalXp { get; init; }
    public List<XpEventDto> RecentEvents { get; init; } = new();
}

public class XpEventDto
{
    public XpEventType EventType { get; init; }
    public int XpAmount { get; init; }
    public DateTime EarnedAt { get; init; }
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
}

public class AwardXpRequest
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public XpEventType EventType { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}

public class LeaderboardDto
{
    public string Type { get; init; } = string.Empty;
    public int? Month { get; init; }
    public int? Year { get; init; }
    public List<LeaderboardEntryDto> Entries { get; init; } = new();
}

public class LeaderboardEntryDto
{
    public int Rank { get; init; }
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public decimal Score { get; init; }
    public string? AvatarUrl { get; init; }
}
