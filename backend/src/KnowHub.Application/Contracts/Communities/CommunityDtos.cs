namespace KnowHub.Application.Contracts;

public class CommunityDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? IconName { get; init; }
    public string? CoverImageUrl { get; init; }
    public int MemberCount { get; init; }
    public bool IsActive { get; init; }
    public bool IsMember { get; init; }
}

public class CreateCommunityRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public string? CoverImageUrl { get; set; }
}

public class UpdateCommunityRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public string? CoverImageUrl { get; set; }
}

public class GetCommunitiesRequest
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
