using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public interface ICurrentUserAccessor
{
    Guid UserId { get; }
    Guid TenantId { get; }
    UserRole Role { get; }
    bool IsInRole(UserRole role);
    /// <summary>True for Admin and SuperAdmin — both have full privileges within their scope.</summary>
    bool IsAdminOrAbove { get; }
}
