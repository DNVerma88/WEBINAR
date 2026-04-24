namespace KnowHub.Application.Contracts;

/// <summary>
/// Provides the current request's TenantId without throwing on unauthenticated requests.
/// Returns <see cref="Guid.Empty"/> when no tenant context is available
/// (e.g. unauthenticated auth endpoints). The EF Core global query filter uses this
/// to allow all rows when TenantId is empty, and to restrict rows to the tenant
/// when a valid TenantId is present.
/// </summary>
public interface ITenantIdProvider
{
    Guid CurrentTenantId { get; }
}
