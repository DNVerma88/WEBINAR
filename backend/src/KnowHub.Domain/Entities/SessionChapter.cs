namespace KnowHub.Domain.Entities;

public class SessionChapter : BaseEntity
{
    public Guid SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TimestampSeconds { get; set; }
    public int OrderSequence { get; set; }

    public Session? Session { get; set; }
}
