using KnowHub.Domain.Enums;

namespace KnowHub.Application.Models.Surveys;

public record CreateSurveyRequest(
    string Title,
    string? Description,
    string? WelcomeMessage,
    string? ThankYouMessage,
    DateTime? EndsAt,
    bool IsAnonymous
);

public record UpdateSurveyRequest(
    string Title,
    string? Description,
    string? WelcomeMessage,
    string? ThankYouMessage,
    DateTime? EndsAt,
    int RecordVersion
);

public record CopySurveyRequest(
    string? NewTitle,
    List<Guid> ExcludeQuestionIds
);

public record AddSurveyQuestionRequest(
    string QuestionText,
    SurveyQuestionType QuestionType,
    List<string>? Options,
    int MinRating,
    int MaxRating,
    bool IsRequired,
    int OrderSequence
);

public record UpdateSurveyQuestionRequest(
    string QuestionText,
    SurveyQuestionType QuestionType,
    List<string>? Options,
    int MinRating,
    int MaxRating,
    bool IsRequired,
    int OrderSequence,
    int RecordVersion
);

public record ReorderQuestionsRequest(
    List<Guid> Ordered
);

public record SubmitSurveyRequest(
    List<SurveyAnswerRequest> Answers
);

public record SurveyAnswerRequest(
    Guid QuestionId,
    string? AnswerText,
    List<string>? AnswerOptions,
    int? RatingValue
);

public record ResendInvitationsRequest(
    List<Guid> UserIds
);

public record GetSurveysRequest(
    string? Status,
    string? SearchTerm,
    int PageNumber = 1,
    int PageSize   = 20
);

public record GetInvitationsRequest(
    string? Status,
    int PageNumber = 1,
    int PageSize   = 20
);

public record GetSurveyResponsesRequest(
    int PageNumber = 1,
    int PageSize   = 20
);
