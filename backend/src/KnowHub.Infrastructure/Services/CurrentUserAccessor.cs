using System.Security.Claims;
using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace KnowHub.Infrastructure.Services;

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId => GetRequiredGuidClaim(ClaimTypes.NameIdentifier);
    public Guid TenantId => GetRequiredGuidClaim("tenantId");
    public UserRole Role => Enum.TryParse<UserRole>(GetClaim("role"), out var role) ? role : UserRole.Employee;

    public bool IsInRole(UserRole role) => (Role & role) == role;
    public bool IsAdminOrAbove => IsInRole(UserRole.Admin) || IsInRole(UserRole.SuperAdmin);

    private Guid GetRequiredGuidClaim(string claimType)
    {
        var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
        if (Guid.TryParse(value, out var id)) return id;
        throw new UnauthorizedAccessException($"Required claim '{claimType}' is missing or invalid.");
    }

    private string? GetClaim(string claimType) =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
}
