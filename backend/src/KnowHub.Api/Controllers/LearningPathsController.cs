using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/learning-paths")]
[Authorize]
public class LearningPathsController : ControllerBase
{
    private readonly ILearningPathService _learningPathService;
    private readonly ILearningPathCohortService _cohortService;

    public LearningPathsController(
        ILearningPathService learningPathService,
        ILearningPathCohortService cohortService)
    {
        _learningPathService = learningPathService;
        _cohortService = cohortService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaths([FromQuery] GetLearningPathsRequest request, CancellationToken cancellationToken)
    {
        var result = await _learningPathService.GetPathsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LearningPathDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPath(Guid id, CancellationToken cancellationToken)
    {
        var result = await _learningPathService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LearningPathDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreatePath([FromBody] CreateLearningPathRequest request, CancellationToken cancellationToken)
    {
        var result = await _learningPathService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPath), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LearningPathDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePath(Guid id, [FromBody] UpdateLearningPathRequest request, CancellationToken cancellationToken)
    {
        var result = await _learningPathService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/enrol")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(Guid id, CancellationToken cancellationToken)
    {
        await _learningPathService.EnrolAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/enrol")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unenrol(Guid id, CancellationToken cancellationToken)
    {
        await _learningPathService.UnenrolAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/progress")]
    [ProducesResponseType(typeof(EnrolmentProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgress(Guid id, CancellationToken cancellationToken)
    {
        var result = await _learningPathService.GetProgressAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/certificate")]
    [ProducesResponseType(typeof(LearningPathCertificateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCertificate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _learningPathService.GetCertificateAsync(id, cancellationToken);
        return Ok(result);
    }

    // -- Cohort endpoints ------------------------------------------------------

    [HttpGet("{id:guid}/cohorts")]
    [ProducesResponseType(typeof(List<LearningPathCohortDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCohorts(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cohortService.GetCohortsAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cohorts")]
    [ProducesResponseType(typeof(LearningPathCohortDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCohort(Guid id, [FromBody] CreateLearningPathCohortRequest request, CancellationToken cancellationToken)
    {
        var result = await _cohortService.CreateCohortAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetCohorts), new { id }, result);
    }

    [HttpPut("{id:guid}/cohorts/{cohortId:guid}")]
    [ProducesResponseType(typeof(LearningPathCohortDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCohort(Guid id, Guid cohortId, [FromBody] UpdateLearningPathCohortRequest request, CancellationToken cancellationToken)
    {
        var result = await _cohortService.UpdateCohortAsync(id, cohortId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/cohorts/{cohortId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCohort(Guid id, Guid cohortId, CancellationToken cancellationToken)
    {
        await _cohortService.DeleteCohortAsync(id, cohortId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(typeof(LearningPathItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddLearningPathItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _learningPathService.AddItemAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetPath), new { id }, result);
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        await _learningPathService.RemoveItemAsync(id, itemId, cancellationToken);
        return NoContent();
    }
}
