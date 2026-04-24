using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class SessionMaterial : BaseEntity
{
    public Guid? SessionId { get; set; }
    public Guid? ProposalId { get; set; }
    public MaterialType MaterialType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long? FileSizeBytes { get; set; }

    public Session? Session { get; set; }
    public SessionProposal? Proposal { get; set; }
}
