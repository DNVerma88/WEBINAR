namespace KnowHub.Application.Contracts.Talent;

public record ScreeningProgressUpdate(
    Guid JobId,
    Guid UserId,
    int Processed,
    int Total,
    int PercentComplete,
    Guid CandidateId,
    string FileName,
    decimal? OverallScore,
    string? Recommendation
);

public interface IScreeningProgressPusher
{
    Task PushProgressAsync(ScreeningProgressUpdate update, CancellationToken ct = default);
    Task PushJobCompletedAsync(Guid jobId, Guid userId, CancellationToken ct = default);
    Task PushJobFailedAsync(Guid jobId, Guid userId, string error, CancellationToken ct = default);
}
