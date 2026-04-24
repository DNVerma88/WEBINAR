namespace KnowHub.Domain.Entities;

public class SessionTag : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid TagId { get; set; }

    public Session Session { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
