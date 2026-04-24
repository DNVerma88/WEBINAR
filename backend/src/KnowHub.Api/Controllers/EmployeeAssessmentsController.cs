using KnowHub.Application.Contracts.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/assessment/assessments")]
[Authorize]
public class EmployeeAssessmentsController : ControllerBase
{
    private readonly IEmployeeAssessmentService _service;
    public EmployeeAssessmentsController(IEmployeeAssessmentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAssessments([FromQuery] AssessmentFilter filter, CancellationToken ct)
        => Ok(await _service.GetAssessmentsAsync(filter, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAssessment(Guid id, CancellationToken ct)
        => Ok(await _service.GetAssessmentByIdAsync(id, ct));

    // Grid/draft/save/submit endpoints are open to all authenticated users:
    // the service layer enforces that each caller can only act on their own group memberships.
    [HttpPost("grid")]
    public async Task<IActionResult> GetOrCreateDrafts([FromBody] AssessmentGridRequest request, CancellationToken ct)
        => Ok(await _service.GetOrCreateDraftsForPeriodAsync(request, ct));

    [HttpPost("draft")]
    public async Task<IActionResult> SaveDraft([FromBody] SaveAssessmentDraftRequest request, CancellationToken ct)
        => Ok(await _service.SaveDraftAsync(request, ct));

    [HttpPost("bulk-save")]
    public async Task<IActionResult> BulkSave([FromBody] BulkSaveAssessmentRequest request, CancellationToken ct)
        => Ok(await _service.BulkSaveDraftsAsync(request, ct));

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        await _service.SubmitAssessmentAsync(id, ct);
        return NoContent();
    }

    [HttpPost("bulk-submit")]
    public async Task<IActionResult> BulkSubmit([FromBody] BulkSubmitRequest request, CancellationToken ct)
    {
        await _service.BulkSubmitAsync(request, ct);
        return NoContent();
    }

    // Reopen requires Manager or above — employees cannot un-submit assessments
    [HttpPost("{id:guid}/reopen")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> Reopen(Guid id, [FromBody] ReopenAssessmentRequest request, CancellationToken ct)
    {
        await _service.ReopenAssessmentAsync(id, request, ct);
        return NoContent();
    }
}
