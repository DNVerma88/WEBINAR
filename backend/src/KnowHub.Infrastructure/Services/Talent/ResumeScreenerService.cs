using System.Text.Json;
using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Talent;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities.Talent;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services.Talent;

public sealed class ResumeScreenerService : IResumeScreenerService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IScreeningJobQueue _queue;
    private readonly StorageProviderFactory _storageFactory;

    public ResumeScreenerService(
        KnowHubDbContext db,
        ICurrentUserAccessor currentUser,
        IScreeningJobQueue queue,
        StorageProviderFactory storageFactory)
    {
        _db = db;
        _currentUser = currentUser;
        _queue = queue;
        _storageFactory = storageFactory;
    }

    public async Task<ScreeningJobDto> CreateJobAsync(CreateScreeningJobRequest request, CancellationToken ct = default)
    {
        var job = new ScreeningJob
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            CreatedByUserId = _currentUser.UserId,
            JobTitle = request.JobTitle,
            JdText = request.JdText,
            Status = ScreeningJobStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        if (request.JdFileReference is not null)
        {
            var provider = _storageFactory.GetProvider(request.JdFileReference.ProviderType);
            var download = await provider.DownloadFileAsync(request.JdFileReference, ct);
            job.JdText = ResumeTextExtractor.ExtractText(download.Content, download.FileName);
            await download.Content.DisposeAsync();
            job.JdFileReference = JsonSerializer.Serialize(request.JdFileReference);
        }

        _db.ScreeningJobs.Add(job);
        await _db.SaveChangesAsync(ct);
        return MapJobToDto(job);
    }

    public async Task<ScreeningJobDetailDto> GetJobAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _db.ScreeningJobs
            .Include(j => j.Candidates)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("ScreeningJob", jobId);

        var candidates = job.Candidates
            .OrderByDescending(c => c.OverallScore)
            .Select(MapCandidateToDto)
            .ToList();

        return new ScreeningJobDetailDto(
            job.Id, job.TenantId, job.CreatedByUserId, job.JobTitle, job.JdText,
            job.PromptTemplate,
            job.Status, job.TotalCandidates, job.ProcessedCandidates, job.ProgressPercent,
            job.ErrorMessage, job.CreatedAt, job.StartedAt, job.CompletedAt, candidates);
    }

    public async Task<PagedResult<ScreeningJobDto>> GetJobsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ScreeningJobs
            .Where(j => j.TenantId == _currentUser.TenantId)
            .OrderByDescending(j => j.CreatedAt)
            .AsNoTracking();

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => MapJobToDto(j))
            .ToListAsync(ct);

        return new PagedResult<ScreeningJobDto>
        {
            Data      = items,
            TotalCount = total,
            PageNumber = page,
            PageSize   = pageSize,
        };
    }

    public async Task AddUploadedFilesAsync(Guid jobId, IEnumerable<UploadedFileInfo> files, CancellationToken ct = default)
    {
        var job = await _db.ScreeningJobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("ScreeningJob", jobId);

        var fileList = files.ToList();
        foreach (var file in fileList)
        {
            _db.ScreeningCandidates.Add(new ScreeningCandidate
            {
                Id = Guid.NewGuid(),
                ScreeningJobId = jobId,
                FileName = file.FileName,
                StorageProviderType = "Local",
                FileReference = file.LocalFilePath,
                Status = CandidateStatus.Queued,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        job.TotalCandidates += fileList.Count;
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddStorageFilesAsync(Guid jobId, IEnumerable<StorageFileReference> fileRefs, CancellationToken ct = default)
    {
        var job = await _db.ScreeningJobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("ScreeningJob", jobId);

        var refList = fileRefs.ToList();
        foreach (var fileRef in refList)
        {
            _db.ScreeningCandidates.Add(new ScreeningCandidate
            {
                Id = Guid.NewGuid(),
                ScreeningJobId = jobId,
                FileName = fileRef.FileName,
                StorageProviderType = fileRef.ProviderType,
                FileReference = JsonSerializer.Serialize(fileRef),
                Status = CandidateStatus.Queued,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        job.TotalCandidates += refList.Count;
        await _db.SaveChangesAsync(ct);
    }

    public async Task StartScreeningAsync(Guid jobId, string scoringMode = ScoringModes.Gemini, string? promptTemplate = null, CancellationToken ct = default)
    {
        var job = await _db.ScreeningJobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("ScreeningJob", jobId);

        if (job.Status != ScreeningJobStatus.Pending)
            throw new BusinessRuleException("Job must be in Pending status to start screening.");

        job.Status = ScreeningJobStatus.Processing;
        job.StartedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(promptTemplate))
            job.PromptTemplate = promptTemplate;
        await _db.SaveChangesAsync(ct);

        _queue.Enqueue(jobId, scoringMode, job.PromptTemplate);
    }

    public async Task ReScreenJobAsync(Guid jobId, string scoringMode, bool overwriteAllScores = false, string? promptTemplate = null, CancellationToken ct = default)
    {
        var job = await _db.ScreeningJobs
            .Include(j => j.Candidates)
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("ScreeningJob", jobId);

        if (job.Status == ScreeningJobStatus.Processing)
            throw new BusinessRuleException("Cannot re-screen a job that is currently processing.");

        // When overwriteAllScores is false, only re-queue candidates that failed or were never
        // processed — leave already-scored candidates untouched.
        var candidatesToReset = overwriteAllScores
            ? job.Candidates.ToList()
            : job.Candidates.Where(c => c.Status != CandidateStatus.Scored).ToList();

        if (candidatesToReset.Count == 0)
            throw new BusinessRuleException("No candidates need re-screening. All candidates are already scored. Enable 'Re-score all candidates' to force a full re-run.");

        // Reset job state
        job.Status = ScreeningJobStatus.Processing;
        job.StartedAt = DateTimeOffset.UtcNow;
        job.CompletedAt = null;
        job.ErrorMessage = null;
        job.ProcessedCandidates = 0;
        job.ProgressPercent = 0;

        foreach (var c in candidatesToReset)
        {
            c.Status = CandidateStatus.Queued;
            c.ErrorMessage = null;
            c.CandidateName = null;
            c.Email = null;
            c.Phone = null;
            c.SemanticSimilarityScore = null;
            c.SkillsDepthScore = null;
            c.LegitimacyScore = null;
            c.OverallScore = null;
            c.Recommendation = null;
            c.ScoreSummary = null;
            c.SkillsMatched = null;
            c.SkillsGap = null;
            c.RedFlags = null;
            c.ScoredAt = null;
        }

        if (!string.IsNullOrWhiteSpace(promptTemplate))
            job.PromptTemplate = promptTemplate;

        await _db.SaveChangesAsync(ct);
        _queue.Enqueue(jobId, scoringMode, job.PromptTemplate);
    }

    public async Task<IReadOnlyList<ScreeningCandidateDto>> GetResultsAsync(Guid jobId, CancellationToken ct = default)
    {
        // Verify tenant ownership
        if (!await _db.ScreeningJobs.AnyAsync(j => j.Id == jobId && j.TenantId == _currentUser.TenantId, ct))
            throw new NotFoundException("ScreeningJob", jobId);

        return await _db.ScreeningCandidates
            .Where(c => c.ScreeningJobId == jobId)
            .OrderByDescending(c => c.OverallScore)
            .AsNoTracking()
            .Select(c => MapCandidateToDto(c))
            .ToListAsync(ct);
    }

    public async Task DeleteJobAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _db.ScreeningJobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("ScreeningJob", jobId);

        if (job.Status == ScreeningJobStatus.Processing)
        {
            // Soft-cancel in-progress jobs
            job.Status = ScreeningJobStatus.Cancelled;
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            _db.ScreeningJobs.Remove(job);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateJdAsync(Guid jobId, string jdText, CancellationToken ct = default)
    {
        var job = await _db.ScreeningJobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("ScreeningJob", jobId);

        if (job.Status == ScreeningJobStatus.Processing)
            throw new BusinessRuleException("Cannot update the job description while screening is in progress.");

        job.JdText = jdText;
        // Clear cached embedding so it is recomputed on next screening run
        job.JdEmbedding = null;

        await _db.SaveChangesAsync(ct);
    }

    private static ScreeningJobDto MapJobToDto(ScreeningJob j) =>
        new(j.Id, j.TenantId, j.CreatedByUserId, j.JobTitle, j.JdText,
            j.Status, j.TotalCandidates, j.ProcessedCandidates, j.ProgressPercent,
            j.ErrorMessage, j.CreatedAt, j.StartedAt, j.CompletedAt);

    private static IReadOnlyList<string>? ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch (JsonException) { return null; }
    }

    private static ScreeningCandidateDto MapCandidateToDto(ScreeningCandidate c) =>
        new(c.Id, c.ScreeningJobId, c.FileName, c.StorageProviderType, c.Status,
            c.ErrorMessage, c.CandidateName, c.Email, c.Phone, c.SemanticSimilarityScore,
            c.SkillsDepthScore, c.LegitimacyScore, c.OverallScore, c.Recommendation,
            c.ScoreSummary, ParseJsonArray(c.SkillsMatched), ParseJsonArray(c.SkillsGap),
            ParseJsonArray(c.RedFlags), c.CreatedAt, c.ScoredAt);
}
