using KnowHub.Application.Models;
using KnowHub.Application.Models.Surveys;

namespace KnowHub.Application.Contracts.Surveys;

public interface ISurveyService
{
    Task<PagedResult<SurveyDto>> GetSurveysAsync(GetSurveysRequest request, CancellationToken ct);
    Task<SurveyDto> GetByIdAsync(Guid surveyId, CancellationToken ct);
    Task<SurveyDto> CreateAsync(CreateSurveyRequest request, CancellationToken ct);
    Task<SurveyDto> UpdateAsync(Guid surveyId, UpdateSurveyRequest request, CancellationToken ct);
    Task DeleteAsync(Guid surveyId, CancellationToken ct);
    Task<SurveyDto> CopyAsync(Guid surveyId, CopySurveyRequest request, CancellationToken ct);
    Task<SurveyDto> LaunchAsync(Guid surveyId, CancellationToken ct);
    Task<SurveyDto> CloseAsync(Guid surveyId, CancellationToken ct);
    Task<SurveyResultsDto> GetResultsAsync(Guid surveyId, CancellationToken ct);

    Task<SurveyQuestionDto> AddQuestionAsync(Guid surveyId, AddSurveyQuestionRequest request, CancellationToken ct);
    Task<SurveyQuestionDto> UpdateQuestionAsync(Guid surveyId, Guid questionId, UpdateSurveyQuestionRequest request, CancellationToken ct);
    Task DeleteQuestionAsync(Guid surveyId, Guid questionId, CancellationToken ct);
    Task ReorderQuestionsAsync(Guid surveyId, ReorderQuestionsRequest request, CancellationToken ct);
}
