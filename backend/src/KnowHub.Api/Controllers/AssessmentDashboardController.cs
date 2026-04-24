using KnowHub.Application.Contracts.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/assessment/dashboard")]
[Authorize]
public class AssessmentDashboardController : ControllerBase
{
    private readonly IEmployeeAssessmentService _service;
    public AssessmentDashboardController(IEmployeeAssessmentService service) => _service = service;

    // Primary-lead dashboard — accessible to any group member who is a lead
    [HttpGet("primary-lead")]
    public async Task<IActionResult> GetPrimaryLeadDashboard([FromQuery] Guid groupId, [FromQuery] Guid? periodId, CancellationToken ct)
        => Ok(await _service.GetPrimaryLeadDashboardAsync(groupId, periodId, ct));

    // Full admin view — Manager/KnowledgeTeam/Admin/SuperAdmin only
    [HttpGet("admin")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> GetAdminDashboard([FromQuery] Guid? periodId, CancellationToken ct)
        => Ok(await _service.GetAdminDashboardAsync(periodId, ct));

    // Co-lead view — accessible to any member who is assigned as co-lead
    [HttpGet("co-lead")]
    public async Task<IActionResult> GetCoLeadDashboard([FromQuery] Guid? periodId, CancellationToken ct)
        => Ok(await _service.GetCoLeadDashboardAsync(periodId, ct));

    // Executive summary — Manager and above only
    [HttpGet("executive")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> GetExecutiveDashboard([FromQuery] Guid? periodId, CancellationToken ct)
        => Ok(await _service.GetExecutiveDashboardAsync(periodId, ct));
}
