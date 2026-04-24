using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public interface ILeaderboardService
{
    Task<LeaderboardDto> GetLeaderboardAsync(LeaderboardType type, int? month, int? year, CancellationToken cancellationToken);
}
