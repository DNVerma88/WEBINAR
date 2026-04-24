using KnowHub.Application.Contracts.Surveys;
using KnowHub.Application.Models.Surveys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KnowHub.Api.Controllers;

/// <summary>
/// Public survey form endpoint — token IS the credential; no JWT required.
/// Deliberately separated from SurveysController to prevent [AllowAnonymous] bleed.
/// </summary>
[ApiController]
[Route("api/surveys/form")]
[AllowAnonymous]
public class SurveyFormController : ControllerBase
{
    private readonly ISurveyResponseService _responseService;

    public SurveyFormController(ISurveyResponseService responseService)
        => _responseService = responseService;

    [HttpGet("{token}")]
    [EnableRateLimiting("SurveyTokenPolicy")]
    public async Task<IActionResult> GetForm(string token, CancellationToken ct)
        => Ok(await _responseService.GetFormByTokenAsync(token, ct));

    [HttpPost("{token}/submit")]
    [EnableRateLimiting("SurveyTokenPolicy")]
    public async Task<IActionResult> Submit(string token, [FromBody] SubmitSurveyRequest request, CancellationToken ct)
    {
        var result = await _responseService.SubmitAsync(token, request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
