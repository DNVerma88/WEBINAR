namespace KnowHub.Application.Contracts.Analytics;

public interface IAnalyticsService
{
    Task<AnalyticsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
    Task<KnowledgeGapHeatmapResponse> GetKnowledgeGapHeatmapAsync(CancellationToken cancellationToken);
    Task<SkillCoverageReportResponse> GetSkillCoverageAsync(CancellationToken cancellationToken);
    Task<ContentFreshnessReportResponse> GetContentFreshnessAsync(CancellationToken cancellationToken);
    Task<LearningFunnelResponse> GetLearningFunnelAsync(CancellationToken cancellationToken);
    Task<CohortCompletionRatesResponse> GetCohortCompletionRatesAsync(CancellationToken cancellationToken);
    Task<DepartmentEngagementScoreResponse> GetDepartmentEngagementAsync(CancellationToken cancellationToken);
    Task<KnowledgeRetentionScoreResponse> GetKnowledgeRetentionAsync(CancellationToken cancellationToken);
}
