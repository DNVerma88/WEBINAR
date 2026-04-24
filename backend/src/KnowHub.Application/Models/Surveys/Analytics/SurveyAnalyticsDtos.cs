using KnowHub.Domain.Enums;

namespace KnowHub.Application.Models.Surveys.Analytics;

public record SurveyAnalyticsSummaryDto(
    Guid   SurveyId,
    string SurveyTitle,
    int    TotalInvited,
    int    TotalSubmitted,
    double ResponseRatePct,
    double AvgCompletionTimeSeconds,
    string HealthStatus               // "Healthy" ≥70%, "AtRisk" 40–69%, "LowEngagement" <40%
);

public record SurveyQuestionAnalyticsDto(
    Guid                              QuestionId,
    string                            QuestionText,
    SurveyQuestionType                QuestionType,
    int                               TotalAnswers,
    IReadOnlyList<OptionStatDto>      OptionStats,
    double?                           AverageRating,
    int?                              MinRating,
    int?                              MaxRating,
    IReadOnlyList<string>             TextAnswers
);

public record OptionStatDto(
    string OptionValue,
    int    Count,
    double Percentage
);

public record DepartmentRowDto(
    string Department,
    double AverageScore,
    int    ResponseCount
);

public record SurveyDepartmentBreakdownDto(
    Guid                              QuestionId,
    string                            QuestionText,
    IReadOnlyList<DepartmentRowDto>   Rows
);

public record SurveyNpsReportDto(
    Guid   SurveyId,
    string SurveyTitle,
    int    Promoters,
    int    Passives,
    int    Detractors,
    int    NpsScore,
    double PromoterPct,
    double PassivePct,
    double DetractorPct
);

public record NpsTrendPointDto(
    Guid     SurveyId,
    string   SurveyTitle,
    DateTime LaunchedAt,
    int      NpsScore
);

public record SurveyNpsTrendDto(
    IReadOnlyList<NpsTrendPointDto> DataPoints
);

public record SurveyParticipationFunnelDto(
    int    TotalInvited,
    int    TotalEmailsSent,
    int    TotalTokensAccessed,
    int    TotalSubmitted,
    double SubmissionRatePct,
    double StartToSubmitRatePct
);

public record SurveyHeatmapDto(
    IReadOnlyList<string> QuestionTexts,
    IReadOnlyList<string> Departments,
    double[][]            Matrix
);

public record SharedQuestionCompDto(
    string                                        QuestionText,
    IReadOnlyList<SurveyQuestionAnalyticsDto>     SurveyStats
);

public record SurveyCompSummaryDto(
    Guid      SurveyId,
    string    Title,
    DateTime? LaunchedAt,
    double    ResponseRatePct
);

public record SurveyComparisonDto(
    IReadOnlyList<SurveyCompSummaryDto>   Surveys,
    IReadOnlyList<SharedQuestionCompDto>  SharedQuestions
);

public record SurveyExportRequest(
    Guid         SurveyId,
    ExportFormat Format,
    bool         IncludeRespondentInfo,
    DateTime?    FromDate,
    DateTime?    ToDate
);

public enum ExportFormat { Csv, Pdf }
