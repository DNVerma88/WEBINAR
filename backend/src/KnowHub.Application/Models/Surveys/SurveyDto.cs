using KnowHub.Domain.Enums;

namespace KnowHub.Application.Models.Surveys;

public record SurveyDto(
    Guid Id,
    Guid TenantId,
    string Title,
    string? Description,
    string? WelcomeMessage,
    string? ThankYouMessage,
    string Status,
    DateTime? EndsAt,
    bool IsAnonymous,
    DateTime? LaunchedAt,
    DateTime? ClosedAt,
    int TotalInvited,
    int TotalResponded,
    int ResponseRate,
    List<SurveyQuestionDto> Questions,
    DateTime CreatedDate,
    Guid CreatedBy
);

public record SurveyQuestionDto(
    Guid Id,
    string QuestionText,
    string QuestionType,
    List<string>? Options,
    int MinRating,
    int MaxRating,
    bool IsRequired,
    int OrderSequence
);

public record SurveyInvitationDto(
    Guid Id,
    Guid UserId,
    string UserFullName,
    string UserEmail,
    string Status,
    DateTime? SentAt,
    DateTime? ExpiresAt,
    DateTime? SubmittedAt,
    int ResendCount
);

public record SurveyResultsDto(
    Guid SurveyId,
    string Title,
    bool IsAnonymous,
    int TotalInvited,
    int TotalResponded,
    int ResponseRatePercent,
    List<QuestionResultDto> QuestionResults
);

public record QuestionResultDto(
    Guid QuestionId,
    string QuestionText,
    string QuestionType,
    int TotalAnswers,
    List<OptionCountDto>? OptionCounts,
    double? AverageRating,
    int? MinRatingGiven,
    int? MaxRatingGiven,
    List<string>? TextAnswers
);

public record OptionCountDto(string OptionLabel, int Count, double PercentageOfResponses);

public record SurveyFormDto(
    Guid SurveyId,
    string Title,
    string? WelcomeMessage,
    string? ThankYouMessage,
    List<SurveyQuestionDto> Questions,
    DateTime ExpiresAt
);

public record SurveyResponseDto(
    Guid Id,
    Guid SurveyId,
    Guid? UserId,           // nullable — masked to null when IsAnonymous = true
    string? UserFullName,   // masked to null when IsAnonymous = true
    DateTime SubmittedAt
);
