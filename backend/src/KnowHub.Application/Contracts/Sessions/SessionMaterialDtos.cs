using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class SessionMaterialDto
{
    public Guid Id { get; init; }
    public Guid? SessionId { get; init; }
    public Guid? ProposalId { get; init; }
    public MaterialType MaterialType { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public long? FileSizeBytes { get; init; }
}

public class AddSessionMaterialRequest
{
    public MaterialType MaterialType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long? FileSizeBytes { get; set; }
}
