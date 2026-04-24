namespace KnowHub.Application.Contracts;

public class TagDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int UsageCount { get; init; }
    public int PostCount { get; init; }
    public bool IsActive { get; init; }
    public bool IsOfficial { get; init; }
    public bool IsFollowedByCurrentUser { get; init; }
}

public class CreateTagRequest
{
    public string Name { get; set; } = string.Empty;
}

public class GetTagsRequest
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
