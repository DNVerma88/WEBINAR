using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;

namespace KnowHub.Tests.TestHelpers;

public class FakeCurrentUserAccessor : ICurrentUserAccessor
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; } = Guid.NewGuid();
    public UserRole Role { get; set; } = UserRole.Employee;

    public bool IsInRole(UserRole role) => Role.HasFlag(role);
    public bool IsAdminOrAbove => IsInRole(UserRole.Admin) || IsInRole(UserRole.SuperAdmin);

    public static FakeCurrentUserAccessor AsEmployee(Guid userId, Guid tenantId) => new()
    {
        UserId = userId,
        TenantId = tenantId,
        Role = UserRole.Employee
    };

    public static FakeCurrentUserAccessor AsContributor(Guid userId, Guid tenantId) => new()
    {
        UserId = userId,
        TenantId = tenantId,
        Role = UserRole.Contributor
    };

    public static FakeCurrentUserAccessor AsManager(Guid userId, Guid tenantId) => new()
    {
        UserId = userId,
        TenantId = tenantId,
        Role = UserRole.Manager
    };

    public static FakeCurrentUserAccessor AsAdmin(Guid userId, Guid tenantId) => new()
    {
        UserId = userId,
        TenantId = tenantId,
        Role = UserRole.Admin
    };

    public static FakeCurrentUserAccessor AsKnowledgeTeam(Guid userId, Guid tenantId) => new()
    {
        UserId = userId,
        TenantId = tenantId,
        Role = UserRole.KnowledgeTeam
    };
}
