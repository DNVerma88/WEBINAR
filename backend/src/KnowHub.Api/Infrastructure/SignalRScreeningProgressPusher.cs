using KnowHub.Api.Hubs;
using KnowHub.Application.Contracts.Talent;
using Microsoft.AspNetCore.SignalR;

namespace KnowHub.Api.Infrastructure;

/// <summary>
/// Pushes real-time screening progress events to connected SignalR clients.
/// </summary>
public sealed class SignalRScreeningProgressPusher : IScreeningProgressPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRScreeningProgressPusher(IHubContext<NotificationHub> hubContext)
        => _hubContext = hubContext;

    public Task PushProgressAsync(ScreeningProgressUpdate update, CancellationToken ct = default) =>
        _hubContext.Clients
            .User(update.UserId.ToString())
            .SendAsync("ReceiveScreeningProgress", new
            {
                jobId           = update.JobId,
                processed       = update.Processed,
                total           = update.Total,
                percentComplete = update.PercentComplete,
                latestCandidate = new
                {
                    candidateId   = update.CandidateId,
                    fileName      = update.FileName,
                    overallScore  = update.OverallScore,
                    recommendation = update.Recommendation,
                }
            }, ct);

    public Task PushJobCompletedAsync(Guid jobId, Guid userId, CancellationToken ct = default) =>
        _hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ScreeningJobCompleted", new { jobId }, ct);

    public Task PushJobFailedAsync(Guid jobId, Guid userId, string error, CancellationToken ct = default) =>
        _hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ScreeningJobFailed", jobId, error, ct);
}
