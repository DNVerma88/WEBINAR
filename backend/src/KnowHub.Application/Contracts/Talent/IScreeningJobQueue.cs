namespace KnowHub.Application.Contracts.Talent;

public record ScreeningQueueItem(Guid JobId, string ScoringMode = ScoringModes.AI, string? PromptTemplate = null);

public interface IScreeningJobQueue
{
    void Enqueue(Guid jobId, string scoringMode = ScoringModes.AI, string? promptTemplate = null);
    IAsyncEnumerable<ScreeningQueueItem> DequeueAllAsync(CancellationToken ct);
}
