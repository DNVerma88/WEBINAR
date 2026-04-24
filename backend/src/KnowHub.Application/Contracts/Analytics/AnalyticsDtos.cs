namespace KnowHub.Application.Contracts.Analytics;

public record AnalyticsSummaryResponse(
    int TotalSessions,
    int TotalAssets,
    int TotalUsers,
    double AvgQuizPassRate,
    int WeeklyActiveUsers,
    List<TopCategoryItem> TopCategories
);

public record TopCategoryItem(Guid Id, string Name, int SessionCount, int AssetCount);

public record HeatmapCell(
    Guid CategoryId,
    string CategoryName,
    string Department,
    double EngagementScore,
    int SessionCount,
    int AssetCount,
    double QuizPassRate
);

public record KnowledgeGapHeatmapResponse(List<HeatmapCell> Cells, List<string> Departments, List<string> Categories);

public record CategorySkillCoverage(
    Guid CategoryId,
    string CategoryName,
    int TotalTagCount,
    int CoveredTagCount,
    double CoveragePercent,
    List<string> TopCoveredSkills,
    List<string> GapSkills
);

public record SkillCoverageReportResponse(List<CategorySkillCoverage> Categories, double OverallCoveragePercent);

public record ContentFreshnessItem(
    Guid Id,
    string ContentType,
    string Title,
    DateTime CreatedDate,
    int AgeDays
);

public record ContentFreshnessReportResponse(
    int FreshCount,
    int RecentCount,
    int AgingCount,
    int StaleCount,
    List<ContentFreshnessItem> StalestItems
);

public record LearningFunnelResponse(
    int Discovered,
    int Registered,
    int Attended,
    int Rated,
    int QuizPassed,
    double RegistrationRate,
    double AttendanceRate,
    double RatingRate,
    double PassRate
);

public record CohortCompletionItem(
    Guid LearningPathId,
    string Title,
    int TotalEnrollments,
    int CompletedCount,
    double CompletionRate,
    double AvgCompletionDays
);

public record CohortCompletionRatesResponse(List<CohortCompletionItem> Items);

public record DepartmentEngagementItem(
    string Department,
    int SessionsAttended,
    int AssetsCreated,
    int TotalXpEarned,
    double AvgQuizPassRate,
    double EngagementScore
);

public record DepartmentEngagementScoreResponse(List<DepartmentEngagementItem> Departments);

public record CategoryRetentionItem(
    Guid CategoryId,
    string CategoryName,
    int QuizAttempts,
    double PassRate,
    double AvgDaysBetweenAttempts,
    double RetentionScore
);

public record KnowledgeRetentionScoreResponse(List<CategoryRetentionItem> Categories, double OverallRetentionScore);
