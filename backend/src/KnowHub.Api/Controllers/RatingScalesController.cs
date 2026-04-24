using KnowHub.Application.Contracts.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/assessment/rating-scales")]
[Authorize]
public class RatingScalesController : ControllerBase
{
    private readonly IRatingScaleService _service;
    public RatingScalesController(IRatingScaleService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetScales(CancellationToken ct)
        => Ok(await _service.GetScalesAsync(ct));

    [HttpPost]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> CreateScale([FromBody] CreateRatingScaleRequest request, CancellationToken ct)
        => Ok(await _service.CreateScaleAsync(request, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> UpdateScale(Guid id, [FromBody] UpdateRatingScaleRequest request, CancellationToken ct)
        => Ok(await _service.UpdateScaleAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> DeactivateScale(Guid id, CancellationToken ct)
    {
        await _service.DeactivateScaleAsync(id, ct);
        return NoContent();
    }
}
