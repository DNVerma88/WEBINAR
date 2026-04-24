using System.Text.Json;
using KnowHub.Application.Models.Surveys;
using KnowHub.Domain.Entities;

namespace KnowHub.Infrastructure.Services.Surveys;

/// <summary>
/// Shared mapping helpers for the Survey domain — eliminates duplication across services.
/// </summary>
internal static class SurveyMappings
{
    internal static SurveyQuestionDto ToDto(SurveyQuestion q)
    {
        var options = q.OptionsJson is not null
            ? JsonSerializer.Deserialize<List<string>>(q.OptionsJson)
            : null;
        return new SurveyQuestionDto(
            q.Id, q.QuestionText, q.QuestionType.ToString(),
            options, q.MinRating, q.MaxRating, q.IsRequired, q.OrderSequence);
    }
}
