using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class KnowledgeRequest : BaseEntity
{
    public Guid RequesterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public int UpvoteCount { get; set; }
    public bool IsAddressed { get; set; }
    public Guid? AddressedBySessionId { get; set; }
    public KnowledgeRequestStatus Status { get; set; } = KnowledgeRequestStatus.Open;
    public int BountyXp { get; set; }
    public Guid? ClaimedByUserId { get; set; }

    public User Requester { get; set; } = null!;
    public Category? Category { get; set; }
    public Session? AddressedBySession { get; set; }
    public User? ClaimedByUser { get; set; }
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}
