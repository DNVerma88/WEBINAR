using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class KnowledgeAsset : BaseEntity
{
    public Guid? SessionId { get; set; }
    public KnowledgeAssetType AssetType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ViewCount { get; set; }
    public int DownloadCount { get; set; }
    public bool IsPublic { get; set; } = true;
    public int VersionNumber { get; set; } = 1;
    public DateTime? ExpiresAt { get; set; }
    public bool IsVerified { get; set; }
    public Guid? VerifiedById { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public Session? Session { get; set; }
    public User? VerifiedBy { get; set; }
}
