namespace KnowHub.Domain.Entities;

public class KnowledgeBundleItem : BaseEntity
{
    public Guid BundleId { get; set; }
    public Guid KnowledgeAssetId { get; set; }
    public int OrderSequence { get; set; }
    public string? Notes { get; set; }

    public KnowledgeBundle? Bundle { get; set; }
    public KnowledgeAsset? KnowledgeAsset { get; set; }
}
