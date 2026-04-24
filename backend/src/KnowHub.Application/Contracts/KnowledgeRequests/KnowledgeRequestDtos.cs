using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class KnowledgeRequestDto
{
    public Guid Id { get; init; }
    public Guid RequesterId { get; init; }
    public string RequesterName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public int UpvoteCount { get; init; }
    public bool IsAddressed { get; init; }
    public KnowledgeRequestStatus Status { get; init; }
    public int BountyXp { get; init; }
    public DateTime CreatedDate { get; init; }
    public bool HasUpvoted { get; init; }
    public Guid? ClaimedByUserId { get; init; }
    public string? ClaimedByName { get; init; }
    public Guid? AddressedBySessionId { get; init; }
}

public class CreateKnowledgeRequestRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public int BountyXp { get; set; }
}

public class CloseKnowledgeRequestRequest
{
    public string? Reason { get; set; }
}

public class AddressKnowledgeRequestRequest
{
    public Guid SessionId { get; set; }
}

public class GetKnowledgeRequestsRequest
{
    public KnowledgeRequestStatus? Status { get; set; }
    public Guid? CategoryId { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
