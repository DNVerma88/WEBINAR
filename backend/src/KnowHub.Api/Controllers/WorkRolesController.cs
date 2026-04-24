using KnowHub.Application.Contracts.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/assessment/work-roles")]
[Authorize]
public class WorkRolesController : ControllerBase
{
    private readonly IWorkRoleService _service;
    public WorkRolesController(IWorkRoleService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetWorkRoles([FromQuery] bool? isActive, CancellationToken ct)
        => Ok(await _service.GetWorkRolesAsync(isActive, ct));

    [HttpPost]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> CreateWorkRole([FromBody] CreateWorkRoleRequest request, CancellationToken ct)
    {
        var result = await _service.CreateWorkRoleAsync(request, ct);
        return CreatedAtAction(nameof(GetWorkRoles), result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> UpdateWorkRole(Guid id, [FromBody] UpdateWorkRoleRequest request, CancellationToken ct)
        => Ok(await _service.UpdateWorkRoleAsync(id, request, ct));
}
