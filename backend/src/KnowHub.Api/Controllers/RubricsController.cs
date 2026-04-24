using KnowHub.Application.Contracts.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/assessment/rubrics")]
[Authorize]
public class RubricsController : ControllerBase
{
    private readonly IRubricService _service;
    public RubricsController(IRubricService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetRubrics([FromQuery] string? designationCode, CancellationToken ct)
        => Ok(await _service.GetRubricsAsync(designationCode, ct));

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentRubrics([FromQuery] string designationCode, CancellationToken ct)
        => Ok(await _service.GetCurrentRubricsForDesignationAsync(designationCode, ct));

    [HttpPost]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> CreateRubric([FromBody] CreateRubricRequest request, CancellationToken ct)
        => Ok(await _service.CreateRubricAsync(request, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> UpdateRubric(Guid id, [FromBody] UpdateRubricRequest request, CancellationToken ct)
        => Ok(await _service.UpdateRubricAsync(id, request, ct));
}
