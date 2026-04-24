using System.Security.Claims;
using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Http;

namespace KnowHub.Infrastructure.Services;

/// <summary>
/// Reads the TenantId claim safely — returns <see cref="Guid.Empty"/> when the
/// HTTP context is absent or the user is unauthenticated. Used by the EF Core
/// global query filter so that unauthenticated endpoints (e.g. /api/auth/login)
/// are never incorrectly filtered.
/// </summary>
public class TenantIdProvider : ITenantIdProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantIdProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid CurrentTenantId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenantId");
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }
}
