using KnowHub.Application.Models;
using KnowHub.Application.Models.Surveys;

namespace KnowHub.Application.Contracts.Surveys;

public interface ISurveyResponseService
{
    /// <summary>Public — token-based, no JWT. Sets TokenAccessedAt if first visit.</summary>
    Task<SurveyFormDto> GetFormByTokenAsync(string plainToken, CancellationToken ct);

    /// <summary>Public — token-based, no JWT. Validates token, saves answers, updates invitation.</summary>
    Task<SurveyResponseDto> SubmitAsync(string plainToken, SubmitSurveyRequest request, CancellationToken ct);

    /// <summary>Admin / SuperAdmin — paginated list of responses for a survey.</summary>
    Task<PagedResult<SurveyResponseDto>> GetResponsesAsync(Guid surveyId, GetSurveyResponsesRequest request, CancellationToken ct);
}
