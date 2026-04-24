namespace KnowHub.Application.Contracts;

public interface IStreakService
{
    Task<UserStreakDto> GetStreakAsync(Guid userId, CancellationToken cancellationToken);
    Task UpdateStreakAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
}
