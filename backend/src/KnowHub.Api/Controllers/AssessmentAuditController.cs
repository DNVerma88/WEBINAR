using KnowHub.Application.Contracts.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/assessment/audit")]
[Authorize(Policy = "ManagerOrAbove")]
public class AssessmentAuditController : ControllerBase
{
    private readonly IAssessmentAuditService _service;
    public AssessmentAuditController(IAssessmentAuditService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditFilter filter, CancellationToken ct)
        => Ok(await _service.GetAuditLogsAsync(filter, ct));
}
