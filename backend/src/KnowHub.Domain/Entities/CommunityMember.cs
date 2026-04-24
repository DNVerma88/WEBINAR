using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class CommunityMember : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid UserId { get; set; }
    public CommunityMemberRole MemberRole { get; set; } = CommunityMemberRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Community Community { get; set; } = null!;
    public User User { get; set; } = null!;
}
