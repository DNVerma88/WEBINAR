using System.Threading.Channels;
using KnowHub.Application.Contracts.Talent;

namespace KnowHub.Infrastructure.Services.Talent;

public sealed class ScreeningJobQueue : IScreeningJobQueue
{
    private readonly Channel<ScreeningQueueItem> _channel = Channel.CreateUnbounded<ScreeningQueueItem>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(Guid jobId, string scoringMode = ScoringModes.AI, string? promptTemplate = null) =>
        _channel.Writer.TryWrite(new ScreeningQueueItem(jobId, scoringMode, promptTemplate));

    public IAsyncEnumerable<ScreeningQueueItem> DequeueAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
