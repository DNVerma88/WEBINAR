namespace KnowHub.Domain.Entities;

public class SkillEndorsement : BaseEntity
{
    public Guid EndorserId { get; set; }
    public Guid EndorseeId { get; set; }
    public Guid TagId { get; set; }
    public Guid SessionId { get; set; }
    public DateTime EndorsedAt { get; set; }

    public User? Endorser { get; set; }
    public User? Endorsee { get; set; }
    public Tag? Tag { get; set; }
    public Session? Session { get; set; }
}
