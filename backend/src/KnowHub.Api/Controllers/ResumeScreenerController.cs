using System.Text;
using System.Text.Json;
using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Talent;
using KnowHub.Infrastructure.AI;
using KnowHub.Infrastructure.Services.Talent;
using KnowHub.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/talent/screening")]
[Authorize]
public class ResumeScreenerController : ControllerBase
{
    private readonly IResumeScreenerService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly TalentModuleConfiguration _talentConfig;
    private readonly StorageConfiguration _storageConfig;
    private readonly AiConfiguration _aiConfig;

    public ResumeScreenerController(
        IResumeScreenerService service,
        ICurrentUserAccessor currentUser,
        IOptions<TalentModuleConfiguration> talentConfig,
        IOptions<StorageConfiguration> storageConfig,
        IOptions<AiConfiguration> aiConfig)
    {
        _service = service;
        _currentUser = currentUser;
        _talentConfig = talentConfig.Value;
        _storageConfig = storageConfig.Value;
        _aiConfig = aiConfig.Value;
    }

    /// <summary>Creates a new screening job.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ScreeningJobDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateJob(
        [FromBody] CreateScreeningJobRequest request, CancellationToken cancellationToken)
    {
        var job = await _service.CreateJobAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetJob), new { jobId = job.Id }, job);
    }

    /// <summary>Returns a paged list of screening jobs for the current tenant.</summary>
    [HttpGet]
    [Authorize(Policy = "ManagerOrAbove")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetJobsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns a single screening job.</summary>
    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(ScreeningJobDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJob(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _service.GetJobAsync(jobId, cancellationToken);
        return Ok(job);
    }

    /// <summary>
    /// Uploads local PDF/DOCX resume files to an existing screening job.
    /// Files are validated (size, extension, magic bytes) before saving.
    /// </summary>
    [HttpPost("{jobId:guid}/upload")]
    [RequestSizeLimit(104_857_600)]
    [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddUploadedFiles(Guid jobId, CancellationToken cancellationToken)
    {
        if (!Request.HasFormContentType)
            return BadRequest("Multipart form data expected.");

        var uploadedFiles = new List<UploadedFileInfo>();
        var savePath = Path.Combine(
            _storageConfig.Local.UploadPath,
            _currentUser.TenantId.ToString(),
            jobId.ToString());

        Directory.CreateDirectory(savePath);

        foreach (var file in Request.Form.Files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!_talentConfig.AllowedFileExtensions.Contains(ext))
                return BadRequest($"File '{file.FileName}' has an unsupported extension. Allowed: {string.Join(", ", _talentConfig.AllowedFileExtensions)}");

            if (file.Length > _talentConfig.MaxFileSizeBytes)
                return BadRequest($"File '{file.FileName}' exceeds the maximum allowed size of {_talentConfig.MaxFileSizeBytes / 1_048_576} MB.");

            // Validate magic bytes before saving
            using (var peekStream = file.OpenReadStream())
            {
                if (!ResumeTextExtractor.IsValidFileType(peekStream, file.FileName))
                    return BadRequest($"File '{file.FileName}' content does not match its declared type.");
            }

            // Use only the file name (no directory components) to prevent path traversal
            var safeFileName = Path.GetFileName(file.FileName);
            var fullPath = Path.Combine(savePath, safeFileName);

            using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                await file.CopyToAsync(fileStream, cancellationToken);

            // FileId is relative to the storage upload root
            var relativeId = Path.Combine(
                _currentUser.TenantId.ToString(), jobId.ToString(), safeFileName)
                .Replace('\\', '/');

            uploadedFiles.Add(new UploadedFileInfo(safeFileName, relativeId, file.Length));
        }

        if (uploadedFiles.Count == 0)
            return BadRequest("No valid files were provided.");

        await _service.AddUploadedFilesAsync(jobId, uploadedFiles, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Adds resumes from cloud storage (S3, Azure Blob, OneDrive, SharePoint) to a job.
    /// </summary>
    [HttpPost("{jobId:guid}/add-from-storage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddStorageFiles(
        Guid jobId, [FromBody] AddStorageFilesRequest request, CancellationToken cancellationToken)
    {
        if (request.Files.Count == 0)
            return BadRequest("No file references provided.");

        var fileRefs = request.Files.Select(f => new StorageFileReference(
            f.ProviderType, f.FileId, f.FileName, f.FileSizeBytes, f.ContainerOrDrive, f.AccessToken));

        await _service.AddStorageFilesAsync(jobId, fileRefs, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Updates the job description text for a screening job.
    /// Clears the cached JD embedding so it is recomputed on the next screening run.
    /// Not allowed while the job is actively Processing.
    /// </summary>
    [HttpPatch("{jobId:guid}/jd")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateJd(
        Guid jobId, [FromBody] UpdateJdRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.JdText))
            return BadRequest("Job description text cannot be empty.");

        if (request.JdText.Length > 100_000)
            return BadRequest("Job description text must not exceed 100 000 characters.");

        await _service.UpdateJdAsync(jobId, request.JdText, cancellationToken);
        return NoContent();
    }

    /// <summary>Starts AI screening for the given job. Optionally specify a scoring mode in the request body.</summary>
    [HttpPost("{jobId:guid}/start")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartScreening(Guid jobId, [FromBody] StartScreeningRequest? request, CancellationToken cancellationToken)
    {
        var mode = request?.ScoringMode ?? ScoringModes.Gemini;
        if (!string.Equals(mode, ScoringModes.AI, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mode, ScoringModes.Gemini, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mode, ScoringModes.Stub, StringComparison.OrdinalIgnoreCase))
            return BadRequest($"ScoringMode must be '{ScoringModes.AI}', '{ScoringModes.Gemini}', or '{ScoringModes.Stub}'.");

        await _service.StartScreeningAsync(jobId, mode, request?.PromptTemplate, cancellationToken);
        return Accepted();
    }

    /// <summary>Returns which AI providers are currently configured in this instance.</summary>
    [HttpGet("ai-status")]
    [ProducesResponseType(typeof(AiProviderStatusDto), StatusCodes.Status200OK)]
    public IActionResult GetAiStatus()
    {
        return Ok(new AiProviderStatusDto(
            OpenAiConfigured: !string.IsNullOrWhiteSpace(_aiConfig.OpenAI.ApiKey) &&
                              _aiConfig.OpenAI.ApiKey != "REPLACE_WITH_OPENAI_API_KEY",
            GeminiConfigured: !string.IsNullOrWhiteSpace(_aiConfig.Gemini.ApiKey) &&
                              _aiConfig.Gemini.ApiKey != "REPLACE_WITH_GEMINI_API_KEY",
            OpenAiModel: _aiConfig.OpenAI.Model,
            GeminiModel: _aiConfig.Gemini.Model
        ));
    }

    /// <summary>Deletes (or cancels) a screening job.</summary>
    [HttpDelete("{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteJob(Guid jobId, CancellationToken cancellationToken)
    {
        await _service.DeleteJobAsync(jobId, cancellationToken);
        return NoContent();
    }

    /// <summary>Re-runs scoring on all candidates with the chosen mode (AI or Stub).</summary>
    [HttpPost("{jobId:guid}/re-screen")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReScreen(
        Guid jobId, [FromBody] ReScreenRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.ScoringMode, ScoringModes.AI, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.ScoringMode, ScoringModes.Gemini, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.ScoringMode, ScoringModes.Stub, StringComparison.OrdinalIgnoreCase))
            return BadRequest($"ScoringMode must be '{ScoringModes.AI}', '{ScoringModes.Gemini}', or '{ScoringModes.Stub}'.");

        await _service.ReScreenJobAsync(jobId, request.ScoringMode, request.OverwriteAllScores, request.PromptTemplate, cancellationToken);
        return Accepted();
    }

    /// <summary>Returns scored candidates for a job, sorted by score descending.</summary>
    [HttpGet("{jobId:guid}/results")]
    [ProducesResponseType(typeof(IReadOnlyList<ScreeningCandidateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResults(Guid jobId, CancellationToken cancellationToken)
    {
        var results = await _service.GetResultsAsync(jobId, cancellationToken);
        return Ok(results);
    }

    /// <summary>Exports screening results as a CSV file.</summary>
    [HttpGet("{jobId:guid}/export-csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportCsv(Guid jobId, CancellationToken cancellationToken)
    {
        var results = await _service.GetResultsAsync(jobId, cancellationToken);
        var csv = BuildCsv(results);
        var bytes = Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "screening-results.csv");
    }

    /// <summary>Returns a single candidate record including scores.</summary>
    [HttpGet("{jobId:guid}/candidates/{candidateId:guid}")]
    [ProducesResponseType(typeof(ScreeningCandidateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCandidate(
        Guid jobId, Guid candidateId, CancellationToken cancellationToken)
    {
        var results = await _service.GetResultsAsync(jobId, cancellationToken);
        var candidate = results.FirstOrDefault(c => c.Id == candidateId);
        return candidate is null ? NotFound() : Ok(candidate);
    }

    // -- Helpers ---------------------------------------------------------------

    private static string BuildCsv(IReadOnlyList<ScreeningCandidateDto> candidates)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Rank,FileName,CandidateName,Email,Phone,OverallScore,Recommendation,SemanticScore,SkillsDepth,Legitimacy,ScoreSummary");

        int rank = 1;
        foreach (var c in candidates)
        {
            sb.Append(rank++).Append(',');
            AppendCsvField(sb, c.FileName);
            AppendCsvField(sb, c.CandidateName);
            AppendCsvField(sb, c.Email);
            AppendCsvField(sb, c.Phone);
            sb.Append(c.OverallScore).Append(',');
            AppendCsvField(sb, c.Recommendation);
            sb.Append(c.SemanticSimilarityScore).Append(',');
            sb.Append(c.SkillsDepthScore).Append(',');
            sb.Append(c.LegitimacyScore).Append(',');
            AppendCsvField(sb, c.ScoreSummary, isLast: true);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Accepts a PDF or DOCX job description file and returns the extracted plain text.
    /// This allows the frontend to obtain JD text from a local file upload without
    /// persisting the file to server storage.
    /// </summary>
    [HttpPost("extract-jd")]
    [RequestSizeLimit(20_971_520)] // 20 MB
    [ProducesResponseType(typeof(ExtractJdTextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtractJdText(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".pdf" and not ".docx")
            return BadRequest("Only .pdf and .docx files are supported.");

        if (file.Length > 20_971_520)
            return BadRequest("File exceeds the 20 MB limit.");

        await using var stream = file.OpenReadStream();

        if (!ResumeTextExtractor.IsValidFileType(stream, file.FileName))
            return BadRequest("File content does not match its declared type.");

        var text = ResumeTextExtractor.ExtractText(stream, file.FileName);
        return Ok(new ExtractJdTextResponse(text));
    }

    private static void AppendCsvField(StringBuilder sb, string? value, bool isLast = false)
    {
        if (value is null)
        {
            if (!isLast) sb.Append(',');
            else sb.AppendLine();
            return;
        }

        var escaped = value.Replace("\"", "\"\"");
        sb.Append('"').Append(escaped).Append('"');
        if (!isLast) sb.Append(',');
        else sb.AppendLine();
    }
}

public record ExtractJdTextResponse(string Text);
