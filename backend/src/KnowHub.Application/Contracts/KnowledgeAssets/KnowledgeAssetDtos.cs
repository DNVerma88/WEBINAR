using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class KnowledgeAssetDto
{
    public Guid Id { get; init; }
    public Guid? SessionId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? Description { get; init; }
    public KnowledgeAssetType AssetType { get; init; }
    public int ViewCount { get; init; }
    public int DownloadCount { get; init; }
    public bool IsPublic { get; init; }
    public bool IsVerified { get; init; }
    public DateTime CreatedDate { get; init; }
    public Guid CreatedBy { get; init; }
}

public class CreateKnowledgeAssetRequest
{
    public Guid? SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public KnowledgeAssetType AssetType { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class GetAssetsRequest
{
    public Guid? SessionId { get; set; }
    public KnowledgeAssetType? AssetType { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
