using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class NotificationDto
{
    public Guid Id { get; init; }
    public NotificationType NotificationType { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedDate { get; init; }
}

public class GetNotificationsRequest
{
    public bool? IsRead { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
