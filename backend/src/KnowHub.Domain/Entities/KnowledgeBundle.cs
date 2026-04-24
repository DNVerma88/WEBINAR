namespace KnowHub.Domain.Entities;

public class KnowledgeBundle : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsPublished { get; set; }
    public string? CoverImageUrl { get; set; }

    public User? CreatedByUser { get; set; }
    public Category? Category { get; set; }
    public ICollection<KnowledgeBundleItem> Items { get; set; } = new List<KnowledgeBundleItem>();
}
