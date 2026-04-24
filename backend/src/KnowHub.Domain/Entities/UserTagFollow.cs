namespace KnowHub.Domain.Entities;

/// <summary>
/// Follows a user or a tag. Exactly one of FollowedUserId / FollowedTagId must be set.
/// </summary>
public class UserTagFollow : BaseEntity
{
    public Guid FollowerId { get; set; }
    public Guid? FollowedUserId { get; set; }
    public Guid? FollowedTagId { get; set; }
    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

    public User? Follower { get; set; }
    public User? FollowedUser { get; set; }
    public Tag? FollowedTag { get; set; }
}
