using KnowHub.Application.Models;
using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts.Moderation;

public record FlagContentRequest(
    FlaggedContentType ContentType,
    Guid ContentId,
    FlagReason Reason,
    string? Notes
);

public record GetContentFlagsRequest(
    FlagStatus? Status,
    FlaggedContentType? ContentType,
    int PageNumber = 1,
    int PageSize = 20
);

public record ReviewFlagRequest(
    FlagStatus Status,
    string? ReviewNotes
);

public record SuspendUserRequest(
    Guid UserId,
    string Reason,
    DateTime? ExpiresAt
);

public record LiftSuspensionRequest(
    string LiftReason
);

public record BulkSessionStatusRequest(
    List<Guid> SessionIds,
    string NewStatus
);

public record ContentFlagDto(
    Guid Id,
    FlaggedContentType ContentType,
    Guid ContentId,
    FlagReason Reason,
    FlagStatus Status,
    string? Notes,
    string FlaggedByUserName,
    string? ReviewedByUserName,
    DateTime? ReviewedAt,
    string? ReviewNotes,
    DateTime CreatedDate
);

public record UserSuspensionDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string SuspendedByUserName,
    string Reason,
    DateTime SuspendedAt,
    DateTime? ExpiresAt,
    bool IsActive,
    string? LiftReason,
    DateTime? LiftedAt
);
