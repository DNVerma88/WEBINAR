using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class ContentFlag : BaseEntity
{
    public Guid FlaggedByUserId { get; set; }
    public FlaggedContentType ContentType { get; set; }
    public Guid ContentId { get; set; }
    public FlagReason Reason { get; set; }
    public string? Notes { get; set; }
    public FlagStatus Status { get; set; } = FlagStatus.Pending;
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    public User? FlaggedBy { get; set; }
    public User? ReviewedBy { get; set; }
}
