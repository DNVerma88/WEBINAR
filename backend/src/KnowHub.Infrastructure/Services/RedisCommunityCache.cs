using KnowHub.Application.Contracts;
using StackExchange.Redis;

namespace KnowHub.Infrastructure.Services;

public class RedisCommunityCache : IDistributedCommunityCache
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCommunityCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    private IDatabase Db => _redis.GetDatabase();

    public async Task IncrementViewAsync(Guid postId, CancellationToken ct = default)
    {
        var key = $"views:buffer:{postId}";
        await Db.StringIncrementAsync(key, 1);
        await Db.KeyExpireAsync(key, TimeSpan.FromMinutes(30));
    }

    public async Task<Dictionary<Guid, long>> FlushViewBufferAsync(CancellationToken ct = default)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var result = new Dictionary<Guid, long>();

        await foreach (var key in server.KeysAsync(pattern: "views:buffer:*"))
        {
            var raw = key.ToString();
            var idStr = raw.Replace("views:buffer:", "");
            if (!Guid.TryParse(idStr, out var postId)) continue;

            var tx = Db.CreateTransaction();
            var getTask = tx.StringGetAsync(key);
            _ = tx.KeyDeleteAsync(key);
            await tx.ExecuteAsync();

            var val = await getTask;
            if (val.IsInteger)
                result[postId] = (long)val;
        }

        return result;
    }

    public async Task InvalidateFeedAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var key = $"feed:{tenantId}:{userId}";
        await Db.KeyDeleteAsync(key);
    }

    public async Task AddToTrendingAsync(Guid tenantId, Guid postId, double score, CancellationToken ct = default)
    {
        var key = $"trending:{tenantId}";
        await Db.SortedSetAddAsync(key, postId.ToString(), score, CommandFlags.FireAndForget);
        await Db.KeyExpireAsync(key, TimeSpan.FromHours(25));
    }

    public async Task<IReadOnlyList<Guid>> GetTrendingPostIdsAsync(Guid tenantId, int count, CancellationToken ct = default)
    {
        var key = $"trending:{tenantId}";
        var entries = await Db.SortedSetRangeByRankAsync(key, 0, count - 1, Order.Descending);
        return entries
            .Where(e => Guid.TryParse(e.ToString(), out _))
            .Select(e => Guid.Parse(e.ToString()))
            .ToList();
    }
}
