using KnowHub.Application.Contracts.Surveys;
using KnowHub.Application.Models.Surveys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

/// <summary>
/// Individual survey responses: paginated list for Admin/SuperAdmin.
/// For anonymous surveys, respondent identity is masked in the service layer.
/// </summary>
[ApiController]
[Route("api/surveys/{surveyId:guid}/responses")]
[Authorize(Policy = "AdminOrAbove")]
public class SurveyResponsesController : ControllerBase
{
    private readonly ISurveyResponseService _responseService;

    public SurveyResponsesController(ISurveyResponseService responseService)
        => _responseService = responseService;

    /// <summary>
    /// Paginated list of individual responses for a survey.
    /// For anonymous surveys, UserId and UserFullName are null in each response.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetResponses(
        Guid surveyId,
        [FromQuery] GetSurveyResponsesRequest request,
        CancellationToken ct)
        => Ok(await _responseService.GetResponsesAsync(surveyId, request, ct));
}
