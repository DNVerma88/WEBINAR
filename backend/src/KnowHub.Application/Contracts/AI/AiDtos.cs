using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts.AI;

public record TranscriptSearchRequest(
    string SearchTerm,
    Guid? CategoryId,
    int PageNumber = 1,
    int PageSize = 20
);

public record TranscriptSearchResultDto(
    Guid SessionId,
    string SessionTitle,
    string SpeakerName,
    DateTime SessionDate,
    string MatchedExcerpt,
    double RelevanceScore
);

public record LearningPathRecommendationDto(
    Guid LearningPathId,
    string Title,
    string Description,
    string DifficultyLevel,
    string RecommendationReason,
    double ConfidenceScore
);

public record KnowledgeGapDto(
    Guid CategoryId,
    string CategoryName,
    string GapDescription,
    int QuizAttempts,
    double PassRate,
    List<string> SuggestedTags,
    List<string> RecommendedActions
);

public record AiSummaryRequest(
    string TranscriptText
);

public record AiSummaryResponse(
    Guid SessionId,
    string Summary,
    List<string> KeyTakeaways,
    List<string> ActionItems,
    DateTime GeneratedAt
);
