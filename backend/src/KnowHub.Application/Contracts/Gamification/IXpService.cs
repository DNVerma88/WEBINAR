using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public interface IXpService
{
    Task<UserXpDto> GetUserXpAsync(Guid userId, CancellationToken cancellationToken);
    Task AwardXpAsync(AwardXpRequest request, CancellationToken cancellationToken);
}
