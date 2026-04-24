namespace KnowHub.Domain.Entities;

/// <summary>Join table — no BaseEntity; uses composite PK.</summary>
public class CommunityPostTag
{
    public Guid PostId { get; set; }
    public Guid TagId { get; set; }
    public Guid TenantId { get; set; }

    public CommunityPost? Post { get; set; }
    public Tag? Tag { get; set; }
}
