using KnowHub.Application.Contracts.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/assessment/periods")]
[Authorize]
public class AssessmentPeriodsController : ControllerBase
{
    private readonly IAssessmentPeriodService _service;
    public AssessmentPeriodsController(IAssessmentPeriodService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetPeriods([FromQuery] AssessmentPeriodFilter filter, CancellationToken ct)
        => Ok(await _service.GetPeriodsAsync(filter, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPeriod(Guid id, CancellationToken ct)
        => Ok(await _service.GetPeriodByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> CreatePeriod([FromBody] CreateAssessmentPeriodRequest request, CancellationToken ct)
    {
        var result = await _service.CreatePeriodAsync(request, ct);
        return CreatedAtAction(nameof(GetPeriod), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> UpdatePeriod(Guid id, [FromBody] UpdateAssessmentPeriodRequest request, CancellationToken ct)
        => Ok(await _service.UpdatePeriodAsync(id, request, ct));

    [HttpPost("{id:guid}/open")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> OpenPeriod(Guid id, CancellationToken ct)
    {
        await _service.OpenPeriodAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> ClosePeriod(Guid id, CancellationToken ct)
    {
        await _service.ClosePeriodAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> PublishPeriod(Guid id, CancellationToken ct)
    {
        await _service.PublishPeriodAsync(id, ct);
        return NoContent();
    }

    [HttpPost("generate")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> GeneratePeriods([FromBody] GeneratePeriodsRequest request, CancellationToken ct)
        => Ok(await _service.GeneratePeriodsAsync(request, ct));
}
