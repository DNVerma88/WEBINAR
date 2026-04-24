using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class PostReaction : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public ReactionType ReactionType { get; set; }

    public CommunityPost? Post { get; set; }
    public User? User { get; set; }
}
