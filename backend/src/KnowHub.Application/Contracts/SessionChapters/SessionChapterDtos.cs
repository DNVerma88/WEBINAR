namespace KnowHub.Application.Contracts;

public class SessionChapterDto
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public string Title { get; init; } = string.Empty;
    public int TimestampSeconds { get; init; }
    public int OrderSequence { get; init; }
}

public class AddChapterRequest
{
    public string Title { get; set; } = string.Empty;
    public int TimestampSeconds { get; set; }
    public int OrderSequence { get; set; }
}
