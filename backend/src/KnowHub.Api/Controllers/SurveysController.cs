using KnowHub.Application.Contracts.Surveys;
using KnowHub.Application.Models.Surveys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

/// <summary>
/// Survey lifecycle: create, read, update, delete, copy, launch, close, results.
/// </summary>
[ApiController]
[Route("api/surveys")]
[Authorize(Policy = "AdminOrAbove")]
public class SurveysController : ControllerBase
{
    private readonly ISurveyService _surveyService;

    public SurveysController(ISurveyService surveyService)
        => _surveyService = surveyService;

    // -- CRUD -----------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> GetSurveys([FromQuery] GetSurveysRequest request, CancellationToken ct)
        => Ok(await _surveyService.GetSurveysAsync(request, ct));

    [HttpPost]
    public async Task<IActionResult> CreateSurvey([FromBody] CreateSurveyRequest request, CancellationToken ct)
    {
        var result = await _surveyService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetSurvey), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSurvey(Guid id, CancellationToken ct)
        => Ok(await _surveyService.GetByIdAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateSurvey(Guid id, [FromBody] UpdateSurveyRequest request, CancellationToken ct)
        => Ok(await _surveyService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSurvey(Guid id, CancellationToken ct)
    {
        await _surveyService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/copy")]
    public async Task<IActionResult> CopySurvey(Guid id, [FromBody] CopySurveyRequest request, CancellationToken ct)
    {
        var result = await _surveyService.CopyAsync(id, request, ct);
        return CreatedAtAction(nameof(GetSurvey), new { id = result.Id }, result);
    }

    // -- Lifecycle ------------------------------------------------------------

    [HttpPost("{id:guid}/launch")]
    public async Task<IActionResult> LaunchSurvey(Guid id, CancellationToken ct)
    {
        await _surveyService.LaunchAsync(id, ct);
        return Accepted(new { message = "Survey launch queued. Invitations will be sent shortly." });
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> CloseSurvey(Guid id, CancellationToken ct)
        => Ok(await _surveyService.CloseAsync(id, ct));

    [HttpGet("{id:guid}/results")]
    public async Task<IActionResult> GetResults(Guid id, CancellationToken ct)
        => Ok(await _surveyService.GetResultsAsync(id, ct));
}
