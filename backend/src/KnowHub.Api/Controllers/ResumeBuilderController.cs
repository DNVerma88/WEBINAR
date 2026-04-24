using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Talent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/talent/resume")]
[Authorize]
public class ResumeBuilderController : ControllerBase
{
    private readonly IResumeBuilderService _service;
    private readonly IResumeParserService _parserService;
    private readonly ICurrentUserAccessor _currentUser;

    public ResumeBuilderController(
        IResumeBuilderService service,
        IResumeParserService parserService,
        ICurrentUserAccessor currentUser)
    {
        _service = service;
        _parserService = parserService;
        _currentUser = currentUser;
    }

    /// <summary>Returns the current user's resume profile.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResumeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await _service.GetProfileAsync(_currentUser.UserId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>Creates or updates the current user's resume profile.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ResumeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveProfile(
        [FromBody] SaveResumeProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await _service.SaveProfileAsync(request, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// Uploads a PDF or DOCX resume, extracts its text in-memory, and uses AI to parse it into
    /// structured fields. Nothing is saved — the parsed data is returned for the user to review
    /// and confirm before saving. Max 10 MB; .pdf and .docx only.
    /// </summary>
    [HttpPost("parse")]
    [ProducesResponseType(typeof(ParsedResumeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ParseResume(CancellationToken cancellationToken)
    {
        if (!Request.HasFormContentType)
            return BadRequest("Request must be multipart/form-data.");

        var file = Request.Form.Files.GetFile("file");
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        // OWASP A05 — extension whitelist
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf" && ext != ".docx")
            return BadRequest("Only PDF (.pdf) and Word (.docx) files are supported.");

        // OWASP A05 — enforce 10 MB cap (matches TalentModuleConfiguration)
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest("File size must not exceed 10 MB.");

        // OWASP A03 — strip directory components to prevent path traversal
        var safeFileName = Path.GetFileName(file.FileName);

        await using var stream = file.OpenReadStream();
        var result = await _parserService.ParseAsync(stream, safeFileName, cancellationToken);

        if (result is null)
            return BadRequest("The file could not be parsed. Ensure it is a valid, non-empty PDF or DOCX document.");

        return Ok(result);
    }

    /// <summary>Downloads the current user's resume as a PDF.</summary>
    [HttpGet("pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(CancellationToken cancellationToken)
    {
        var stream = await _service.GeneratePdfAsync(_currentUser.UserId, cancellationToken);
        return File(stream, "application/pdf", "resume.pdf");
    }

    /// <summary>Downloads the current user's resume as a DOCX file.</summary>
    [HttpGet("word")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadWord(CancellationToken cancellationToken)
    {
        var stream = await _service.GenerateWordAsync(_currentUser.UserId, cancellationToken);
        return File(stream,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "resume.docx");
    }

    // -- Admin endpoints ------------------------------------------------------

    /// <summary>Returns a summary list of all users and their resume status. Admin/SuperAdmin only.</summary>
    [HttpGet("admin/all")]
    [Authorize(Policy = "AdminOrAbove")]
    [ProducesResponseType(typeof(IReadOnlyList<ResumeProfileAdminSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllProfileSummaries(CancellationToken cancellationToken)
    {
        var summaries = await _service.GetAllProfileSummariesAsync(cancellationToken);
        return Ok(summaries);
    }

    /// <summary>Downloads a specific user's resume as PDF. Admin/SuperAdmin only.</summary>
    [HttpGet("admin/{userId:guid}/pdf")]
    [Authorize(Policy = "AdminOrAbove")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminDownloadPdf(Guid userId, CancellationToken cancellationToken)
    {
        var stream = await _service.GeneratePdfAsync(userId, cancellationToken);
        return File(stream, "application/pdf", "resume.pdf");
    }

    /// <summary>Downloads a specific user's resume as DOCX. Admin/SuperAdmin only.</summary>
    [HttpGet("admin/{userId:guid}/word")]
    [Authorize(Policy = "AdminOrAbove")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminDownloadWord(Guid userId, CancellationToken cancellationToken)
    {
        var stream = await _service.GenerateWordAsync(userId, cancellationToken);
        return File(stream,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "resume.docx");
    }

    /// <summary>Returns a specific user's full resume profile for admin viewing/editing. Admin/SuperAdmin only.</summary>
    [HttpGet("admin/{userId:guid}")]
    [Authorize(Policy = "AdminOrAbove")]
    [ProducesResponseType(typeof(ResumeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminGetProfile(Guid userId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            return Forbid();

        var profile = await _service.GetProfileForAdminAsync(userId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>Creates or updates a specific user's resume profile on behalf of an admin. Admin/SuperAdmin only.</summary>
    [HttpPut("admin/{userId:guid}")]
    [ProducesResponseType(typeof(ResumeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminSaveProfile(
        Guid userId, [FromBody] SaveResumeProfileRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            return Forbid();

        var profile = await _service.SaveProfileForAdminAsync(userId, request, cancellationToken);
        return Ok(profile);
    }
}
