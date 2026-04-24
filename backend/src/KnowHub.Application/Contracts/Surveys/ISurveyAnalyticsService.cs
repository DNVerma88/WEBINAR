using KnowHub.Application.Models.Surveys.Analytics;

namespace KnowHub.Application.Contracts.Surveys;

public interface ISurveyAnalyticsService
{
    Task<SurveyAnalyticsSummaryDto> GetDashboardAsync(Guid surveyId, CancellationToken ct = default);

    Task<IReadOnlyList<SurveyQuestionAnalyticsDto>> GetQuestionStatsAsync(
        Guid      surveyId,
        string?   departmentFilter,
        string?   roleFilter,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default);

    Task<SurveyDepartmentBreakdownDto> GetDepartmentBreakdownAsync(Guid surveyId, Guid questionId, CancellationToken ct = default);

    Task<SurveyNpsReportDto> GetNpsReportAsync(Guid surveyId, CancellationToken ct = default);

    Task<SurveyNpsTrendDto> GetNpsTrendAsync(IReadOnlyList<Guid> surveyIds, CancellationToken ct = default);

    Task<SurveyParticipationFunnelDto> GetParticipationFunnelAsync(Guid surveyId, CancellationToken ct = default);

    Task<SurveyHeatmapDto> GetHeatmapAsync(Guid surveyId, CancellationToken ct = default);

    Task<SurveyComparisonDto> CompareSurveysAsync(Guid surveyIdA, Guid surveyIdB, CancellationToken ct = default);

    Task<(byte[] Data, string FileName)> ExportToCsvAsync(SurveyExportRequest request, CancellationToken ct = default);

    Task<(byte[] Data, string FileName)> ExportToPdfAsync(SurveyExportRequest request, CancellationToken ct = default);
}
