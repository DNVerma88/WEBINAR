using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class LearningPathItem : BaseEntity
{
    public Guid LearningPathId { get; set; }
    public LearningPathItemType ItemType { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? KnowledgeAssetId { get; set; }
    public int OrderSequence { get; set; }
    public bool IsRequired { get; set; } = true;

    public LearningPath? LearningPath { get; set; }
    public Session? Session { get; set; }
    public KnowledgeAsset? KnowledgeAsset { get; set; }
}
