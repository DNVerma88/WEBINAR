using KnowHub.Application.Contracts.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/assessment/parameters")]
[Authorize]
public class ParametersController : ControllerBase
{
    private readonly IParameterService _service;
    public ParametersController(IParameterService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetParameters(CancellationToken ct)
        => Ok(await _service.GetParametersAsync(ct));

    [HttpPost]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> CreateParameter([FromBody] CreateParameterRequest request, CancellationToken ct)
        => Ok(await _service.CreateParameterAsync(request, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> UpdateParameter(Guid id, [FromBody] UpdateParameterRequest request, CancellationToken ct)
        => Ok(await _service.UpdateParameterAsync(id, request, ct));

    [HttpGet("role-mappings")]
    public async Task<IActionResult> GetRoleMappings([FromQuery] string? designationCode, CancellationToken ct)
        => Ok(await _service.GetRoleMappingsAsync(designationCode, ct));

    [HttpPost("role-mappings")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> UpsertRoleMapping([FromBody] UpsertRoleMappingRequest request, CancellationToken ct)
        => Ok(await _service.UpsertRoleMappingAsync(request, ct));

    [HttpDelete("role-mappings/{id:guid}")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> RemoveRoleMapping(Guid id, CancellationToken ct)
    {
        await _service.RemoveRoleMappingAsync(id, ct);
        return NoContent();
    }
}
