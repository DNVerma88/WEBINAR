namespace KnowHub.Domain.Entities;

public class UserSkill : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid TagId { get; set; }

    public User User { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
