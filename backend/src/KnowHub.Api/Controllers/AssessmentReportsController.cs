using KnowHub.Application.Contracts.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/assessment/reports")]
[Authorize(Policy = "ManagerOrAbove")]
public class AssessmentReportsController : ControllerBase
{
    private readonly IAssessmentReportService _service;
    public AssessmentReportsController(IAssessmentReportService service) => _service = service;

    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedReport([FromQuery] DetailedReportFilter filter, CancellationToken ct)
        => Ok(await _service.GetDetailedReportAsync(filter, ct));

    [HttpGet("completion")]
    public async Task<IActionResult> GetCompletionReport([FromQuery] Guid? periodId, [FromQuery] Guid? groupId, CancellationToken ct)
        => Ok(await _service.GetCompletionReportAsync(periodId, groupId, ct));

    [HttpGet("group-distribution")]
    public async Task<IActionResult> GetGroupDistribution([FromQuery] Guid periodId, CancellationToken ct)
        => Ok(await _service.GetGroupDistributionAsync(periodId, ct));

    [HttpGet("role-distribution")]
    public async Task<IActionResult> GetRoleDistribution([FromQuery] Guid periodId, CancellationToken ct)
        => Ok(await _service.GetRoleDistributionAsync(periodId, ct));

    [HttpGet("employee-history")]
    public async Task<IActionResult> GetEmployeeHistory([FromQuery] Guid userId, CancellationToken ct)
        => Ok(await _service.GetEmployeeHistoryAsync(userId, ct));

    [HttpGet("trend")]
    public async Task<IActionResult> GetTrendReport([FromQuery] TrendReportFilter filter, CancellationToken ct)
        => Ok(await _service.GetTrendReportAsync(filter, ct));

    [HttpGet("risk")]
    public async Task<IActionResult> GetRiskReport([FromQuery] Guid periodId, CancellationToken ct)
        => Ok(await _service.GetRiskReportAsync(periodId, ct));

    [HttpGet("improvement")]
    public async Task<IActionResult> GetImprovementReport([FromQuery] ImprovementReportFilter filter, CancellationToken ct)
        => Ok(await _service.GetImprovementReportAsync(filter, ct));

    [HttpGet("work-role-rating")]
    public async Task<IActionResult> GetWorkRoleRating([FromQuery] Guid? periodId, [FromQuery] Guid? groupId, CancellationToken ct)
        => Ok(await _service.GetWorkRoleRatingReportAsync(periodId, groupId, ct));

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] ExportFilter filter, CancellationToken ct)
    {
        var bytes = await _service.ExportToCsvAsync(filter, ct);
        return File(bytes, "text/csv", "assessment-export.csv");
    }
}
