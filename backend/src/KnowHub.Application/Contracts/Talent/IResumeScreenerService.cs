using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts.Talent;

public interface IResumeScreenerService
{
    Task<ScreeningJobDto> CreateJobAsync(CreateScreeningJobRequest request, CancellationToken ct = default);
    Task<ScreeningJobDetailDto> GetJobAsync(Guid jobId, CancellationToken ct = default);
    Task<PagedResult<ScreeningJobDto>> GetJobsAsync(int page, int pageSize, CancellationToken ct = default);
    Task AddUploadedFilesAsync(Guid jobId, IEnumerable<UploadedFileInfo> files, CancellationToken ct = default);
    Task AddStorageFilesAsync(Guid jobId, IEnumerable<StorageFileReference> fileRefs, CancellationToken ct = default);
    Task StartScreeningAsync(Guid jobId, string scoringMode = ScoringModes.Gemini, string? promptTemplate = null, CancellationToken ct = default);
    Task ReScreenJobAsync(Guid jobId, string scoringMode, bool overwriteAllScores = false, string? promptTemplate = null, CancellationToken ct = default);
    Task<IReadOnlyList<ScreeningCandidateDto>> GetResultsAsync(Guid jobId, CancellationToken ct = default);
    Task DeleteJobAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>Updates the job description text for a screening job. The cached JD embedding is cleared so it is recomputed on next screening run. Not allowed while the job is Processing.</summary>
    Task UpdateJdAsync(Guid jobId, string jdText, CancellationToken ct = default);
}
