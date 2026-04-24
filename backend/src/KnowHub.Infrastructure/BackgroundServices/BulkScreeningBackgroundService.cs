using System.Text.Json;
using KnowHub.Application.Contracts.Talent;
using KnowHub.Domain.Enums;
using KnowHub.Infrastructure.AI;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Services.Talent;
using KnowHub.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowHub.Infrastructure.BackgroundServices;

public sealed class BulkScreeningBackgroundService : BackgroundService
{
    private readonly IScreeningJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IScreeningProgressPusher _progressPusher;
    private readonly AiConfiguration _aiConfig;
    private readonly TalentModuleConfiguration _config;
    private readonly ILogger<BulkScreeningBackgroundService> _logger;

    public BulkScreeningBackgroundService(
        IScreeningJobQueue queue,
        IServiceScopeFactory scopeFactory,
        IScreeningProgressPusher progressPusher,
        IOptions<AiConfiguration> aiConfig,
        IOptions<TalentModuleConfiguration> config,
        ILogger<BulkScreeningBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _progressPusher = progressPusher;
        _aiConfig = aiConfig.Value;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(item.JobId, item.ScoringMode, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process screening job {JobId}", item.JobId);
                await PersistJobFailureAsync(item.JobId, ex.Message, stoppingToken);
            }
        }
    }

    private async Task PersistJobFailureAsync(Guid jobId, string errorMessage, CancellationToken ct)
    {
        try
        {
            Guid userId;
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<KnowHubDbContext>();
                var job = await db.ScreeningJobs.FindAsync([jobId], ct);
                if (job is null) return;

                userId = job.CreatedByUserId;
                await db.ScreeningJobs
                    .Where(j => j.Id == jobId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(j => j.Status, ScreeningJobStatus.Failed)
                        .SetProperty(j => j.ErrorMessage, errorMessage)
                        .SetProperty(j => j.CompletedAt, DateTimeOffset.UtcNow), ct);
            }

            await _progressPusher.PushJobFailedAsync(jobId, userId, errorMessage, ct);
        }
        catch (Exception persistEx)
        {
            _logger.LogError(persistEx, "Failed to persist failure state for screening job {JobId}", jobId);
        }
    }

    private async Task ProcessJobAsync(Guid jobId, string scoringMode, CancellationToken ct)
    {
        _logger.LogInformation("Processing screening job {JobId} with scoring mode '{ScoringMode}'", jobId, scoringMode);

        JobSnapshot? snapshot;
        using (var loadScope = _scopeFactory.CreateScope())
        {
            var db = loadScope.ServiceProvider.GetRequiredService<KnowHubDbContext>();
            var job = await db.ScreeningJobs
                .Include(j => j.Candidates)
                .FirstOrDefaultAsync(j => j.Id == jobId, ct);

            if (job is null)
            {
                _logger.LogWarning("Screening job {JobId} not found; skipping.", jobId);
                return;
            }

            snapshot = new JobSnapshot(
                job.Id,
                job.CreatedByUserId,
                job.JdText ?? "",
                job.PromptTemplate,
                job.Candidates.Where(c => c.Status == CandidateStatus.Queued)
                    .Select(c => new CandidateSnapshot(c.Id, c.FileName, c.StorageProviderType, c.FileReference))
                    .ToList());

            job.TotalCandidates = snapshot.Candidates.Count;
            await db.SaveChangesAsync(ct);
        }

        // Compute JD embedding once (only needed for OpenAI mode)
        float[] jdEmbedding = [];
        if (string.Equals(scoringMode, ScoringModes.AI, StringComparison.OrdinalIgnoreCase) && IsOpenAiConfigured)
        {
            using var embScope = _scopeFactory.CreateScope();
            var scorer = embScope.ServiceProvider.GetRequiredService<ResumeScorer>();
            jdEmbedding = await scorer.ComputeEmbeddingAsync(snapshot.JdText, _aiConfig.OpenAI.ApiKey, ct);
        }

        // Process candidates in parallel within the concurrency limit
        int processed = 0;
        var sem = new SemaphoreSlim(_config.MaxConcurrentScreening);
        var tasks = snapshot.Candidates.Select(cand => Task.Run(async () =>
        {
            await sem.WaitAsync(ct);
            try
            {
                decimal? overallScore = null;
                string? recommendation = null;

                using (var scope = _scopeFactory.CreateScope())
                {
                    (overallScore, recommendation) = await ProcessCandidateAsync(scope, snapshot.JdText, jdEmbedding, cand, scoringMode, snapshot.PromptTemplate, ct);
                }

                var p   = Interlocked.Increment(ref processed);
                var pct = (int)((double)p / snapshot.Candidates.Count * 100);

                await _progressPusher.PushProgressAsync(new ScreeningProgressUpdate(
                    snapshot.JobId,
                    snapshot.UserId,
                    p,
                    snapshot.Candidates.Count,
                    pct,
                    cand.Id,
                    cand.FileName,
                    overallScore,
                    recommendation), ct);
            }
            finally
            {
                sem.Release();
            }
        }, ct)).ToList();

        await Task.WhenAll(tasks);

        // Mark job completed
        using (var completeScope = _scopeFactory.CreateScope())
        {
            var db = completeScope.ServiceProvider.GetRequiredService<KnowHubDbContext>();
            await db.ScreeningJobs
                .Where(j => j.Id == jobId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.Status, ScreeningJobStatus.Completed)
                    .SetProperty(j => j.CompletedAt, DateTimeOffset.UtcNow)
                    .SetProperty(j => j.ProgressPercent, 100)
                    .SetProperty(j => j.ProcessedCandidates, snapshot.Candidates.Count), ct);
        }

        await _progressPusher.PushJobCompletedAsync(jobId, snapshot.UserId, ct);
        _logger.LogInformation("Screening job {JobId} completed with {Count} candidates", jobId, snapshot.Candidates.Count);
    }

    private async Task<(decimal? OverallScore, string? Recommendation)> ProcessCandidateAsync(
        IServiceScope scope,
        string jdText,
        float[] jdEmbedding,
        CandidateSnapshot cand,
        string scoringMode,
        string? promptTemplate,
        CancellationToken ct)
    {
        var db             = scope.ServiceProvider.GetRequiredService<KnowHubDbContext>();
        var storageFactory = scope.ServiceProvider.GetRequiredService<StorageProviderFactory>();
        var scorer         = scope.ServiceProvider.GetRequiredService<ResumeScorer>();

        var candidate = await db.ScreeningCandidates.FindAsync([cand.Id], ct);
        if (candidate is null) return (null, null);

        candidate.Status = CandidateStatus.Processing;
        await db.SaveChangesAsync(ct);

        try
        {
            // Download file from storage
            StorageDownloadResult download;
            if (cand.StorageProviderType == "Local")
            {
                var fileRef = new StorageFileReference("Local", cand.FileReference, cand.FileName, 0);
                download = await storageFactory.GetProvider("Local").DownloadFileAsync(fileRef, ct);
            }
            else
            {
                var fileRef = JsonSerializer.Deserialize<StorageFileReference>(cand.FileReference)!;
                download = await storageFactory.GetProvider(fileRef.ProviderType).DownloadFileAsync(fileRef, ct);
            }

            string resumeText;
            using (download.Content)
                resumeText = ResumeTextExtractor.ExtractText(download.Content, cand.FileName);

            // Guard: if we couldn't extract any meaningful text the file is likely a scanned/
            // image-based PDF.  Send the candidate to Failed with a clear human-readable
            // message rather than passing an empty string to the AI which would produce
            // misleading "resume is blank" commentary.
            if (string.IsNullOrWhiteSpace(resumeText) || resumeText.Trim().Length < 50)
            {
                candidate.Status       = CandidateStatus.Failed;
                candidate.ErrorMessage = "No text could be extracted from this resume. The file is likely a scanned or image-based PDF. Please upload a searchable (text-based) PDF or DOCX file.";
                await db.SaveChangesAsync(ct);
                return (null, null);
            }

            // Score
            var result = await scorer.ScoreAsync(jdText, jdEmbedding, resumeText, _aiConfig, ct, scoringMode, promptTemplate);

            candidate.Status                  = CandidateStatus.Scored;
            candidate.ExtractedText           = resumeText.Length > 50_000 ? resumeText[..50_000] : resumeText;
            candidate.CandidateName           = result.CandidateName;
            candidate.Email                   = result.Email;
            candidate.Phone                   = result.Phone;
            candidate.SemanticSimilarityScore = result.SemanticSimilarityScore;
            candidate.SkillsDepthScore        = result.SkillsDepthScore;
            candidate.LegitimacyScore         = result.LegitimacyScore;
            candidate.OverallScore            = result.OverallScore;
            candidate.Recommendation          = result.Recommendation;
            candidate.ScoreSummary            = result.ScoreSummary;
            candidate.SkillsMatched           = result.SkillsMatched.Count > 0 ? JsonSerializer.Serialize(result.SkillsMatched) : null;
            candidate.SkillsGap               = result.SkillsGap.Count > 0 ? JsonSerializer.Serialize(result.SkillsGap) : null;
            candidate.RedFlags                = result.RedFlags.Count > 0 ? JsonSerializer.Serialize(result.RedFlags) : null;
            candidate.ScoredAt                = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            return (result.OverallScore, result.Recommendation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process candidate {CandidateId} ({FileName}) in job {JobId}: {ErrorType}: {ErrorMessage}",
                cand.Id, cand.FileName, candidate.ScreeningJobId, ex.GetType().Name, ex.Message);
            candidate.Status       = CandidateStatus.Failed;
            candidate.ErrorMessage = ex is FileNotFoundException
                ? $"Resume file '{cand.FileName}' not found — the server was restarted and local uploads were cleared. Please upload the file again."
                : $"{ex.GetType().Name}: {ex.Message}";
            await db.SaveChangesAsync(ct);
            return (null, null);
        }
    }

    private bool IsOpenAiConfigured =>
        !string.IsNullOrWhiteSpace(_aiConfig.OpenAI.ApiKey) &&
        _aiConfig.OpenAI.ApiKey != "REPLACE_WITH_OPENAI_API_KEY";

    private record JobSnapshot(Guid JobId, Guid UserId, string JdText, string? PromptTemplate, List<CandidateSnapshot> Candidates);
    private record CandidateSnapshot(Guid Id, string FileName, string StorageProviderType, string FileReference);
}
