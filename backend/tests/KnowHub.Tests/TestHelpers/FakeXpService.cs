using KnowHub.Application.Contracts;

namespace KnowHub.Tests.TestHelpers;

public class FakeXpService : IXpService
{
    public List<AwardXpRequest> AwardedXp { get; } = new();

    public Task<UserXpDto> GetUserXpAsync(Guid userId, CancellationToken cancellationToken)
        => Task.FromResult(new UserXpDto { UserId = userId, TotalXp = 0, RecentEvents = new() });

    public Task AwardXpAsync(AwardXpRequest request, CancellationToken cancellationToken)
    {
        AwardedXp.Add(request);
        return Task.CompletedTask;
    }
}
