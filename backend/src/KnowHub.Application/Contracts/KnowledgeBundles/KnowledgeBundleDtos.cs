namespace KnowHub.Application.Contracts;

public class KnowledgeBundleDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public bool IsPublished { get; init; }
    public string? CoverImageUrl { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public DateTime CreatedDate { get; init; }
}

public class KnowledgeBundleItemDto
{
    public Guid Id { get; init; }
    public Guid KnowledgeAssetId { get; init; }
    public string AssetTitle { get; init; } = string.Empty;
    public string AssetUrl { get; init; } = string.Empty;
    public string AssetType { get; init; } = string.Empty;
    public int OrderSequence { get; init; }
    public string? Notes { get; init; }
}

public class KnowledgeBundleDetailDto : KnowledgeBundleDto
{
    public List<KnowledgeBundleItemDto> Items { get; init; } = new();
}

public class CreateKnowledgeBundleRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CoverImageUrl { get; set; }
}

public class UpdateKnowledgeBundleRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsPublished { get; set; }
}

public class AddBundleItemRequest
{
    public Guid KnowledgeAssetId { get; set; }
    public int OrderSequence { get; set; }
    public string? Notes { get; set; }
}

public class GetBundlesRequest
{
    public Guid? CategoryId { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
