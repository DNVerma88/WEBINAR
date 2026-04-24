using KnowHub.Application.Contracts.Surveys;
using KnowHub.Application.Models.Surveys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

/// <summary>
/// Survey question management: add, update, delete, reorder.
/// All operations are restricted to Draft surveys (enforced in service layer).
/// </summary>
[ApiController]
[Route("api/surveys/{surveyId:guid}/questions")]
[Authorize(Policy = "AdminOrAbove")]
public class SurveyQuestionsController : ControllerBase
{
    private readonly ISurveyService _surveyService;

    public SurveyQuestionsController(ISurveyService surveyService)
        => _surveyService = surveyService;

    [HttpPost]
    public async Task<IActionResult> AddQuestion(
        Guid surveyId,
        [FromBody] AddSurveyQuestionRequest request,
        CancellationToken ct)
    {
        var result = await _surveyService.AddQuestionAsync(surveyId, request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{questionId:guid}")]
    public async Task<IActionResult> UpdateQuestion(
        Guid surveyId,
        Guid questionId,
        [FromBody] UpdateSurveyQuestionRequest request,
        CancellationToken ct)
        => Ok(await _surveyService.UpdateQuestionAsync(surveyId, questionId, request, ct));

    [HttpDelete("{questionId:guid}")]
    public async Task<IActionResult> DeleteQuestion(
        Guid surveyId,
        Guid questionId,
        CancellationToken ct)
    {
        await _surveyService.DeleteQuestionAsync(surveyId, questionId, ct);
        return NoContent();
    }

    /// <summary>
    /// Reorder all questions. Body: ordered array of question GUIDs.
    /// The position in the array becomes the new OrderSequence.
    /// </summary>
    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderQuestions(
        Guid surveyId,
        [FromBody] ReorderQuestionsRequest request,
        CancellationToken ct)
    {
        await _surveyService.ReorderQuestionsAsync(surveyId, request, ct);
        return NoContent();
    }
}
