namespace KnowHub.Domain.Entities;

public class EmailLog : BaseEntity
{
    public string RecipientEmail { get; set; } = string.Empty;
    public Guid? RecipientUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string EmailType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; }

    public User? RecipientUser { get; set; }
}
