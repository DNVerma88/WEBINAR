import axiosClient from '../../../shared/api/axiosClient';
import type {
  SurveyAnalyticsSummaryDto,
  SurveyQuestionAnalyticsDto,
  SurveyDepartmentBreakdownDto,
  SurveyNpsReportDto,
  SurveyNpsTrendDto,
  SurveyParticipationFunnelDto,
  SurveyHeatmapDto,
  SurveyComparisonDto,
  QuestionStatsFilters,
} from '../types';

const BASE = '/surveys';

function triggerDownload(data: Blob, fileName: string, mimeType: string): void {
  const url = URL.createObjectURL(new Blob([data], { type: mimeType }));
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

export const surveyAnalyticsApi = {
  getDashboard: (surveyId: string) =>
    axiosClient
      .get<SurveyAnalyticsSummaryDto>(`${BASE}/${surveyId}/analytics/dashboard`)
      .then(r => r.data),

  getQuestionStats: (surveyId: string, filters?: QuestionStatsFilters) =>
    axiosClient
      .get<SurveyQuestionAnalyticsDto[]>(`${BASE}/${surveyId}/analytics/questions`, { params: filters })
      .then(r => r.data),

  getDepartmentBreakdown: (surveyId: string, questionId: string) =>
    axiosClient
      .get<SurveyDepartmentBreakdownDto>(
        `${BASE}/${surveyId}/analytics/questions/${questionId}/department-breakdown`,
      )
      .then(r => r.data),

  getNpsReport: (surveyId: string) =>
    axiosClient
      .get<SurveyNpsReportDto>(`${BASE}/${surveyId}/analytics/nps`)
      .then(r => r.data),

  getNpsTrend: (surveyIds: string[]) =>
    axiosClient
      .get<SurveyNpsTrendDto>(`${BASE}/analytics/nps-trend`, {
        params: { surveyIds: surveyIds.join(',') },
      })
      .then(r => r.data),

  getParticipationFunnel: (surveyId: string) =>
    axiosClient
      .get<SurveyParticipationFunnelDto>(`${BASE}/${surveyId}/analytics/funnel`)
      .then(r => r.data),

  getHeatmap: (surveyId: string) =>
    axiosClient
      .get<SurveyHeatmapDto>(`${BASE}/${surveyId}/analytics/heatmap`)
      .then(r => r.data),

  compareSurveys: (a: string, b: string) =>
    axiosClient
      .get<SurveyComparisonDto>(`${BASE}/analytics/compare`, { params: { a, b } })
      .then(r => r.data),

  exportCsv: async (surveyId: string, includeRespondentInfo = false): Promise<void> => {
    const resp = await axiosClient.get(`${BASE}/${surveyId}/export`, {
      params: { format: 'csv', includeRespondentInfo },
      responseType: 'blob',
    });
    triggerDownload(resp.data as Blob, `survey-${surveyId}-responses.csv`, 'text/csv');
  },

  exportPdf: async (surveyId: string): Promise<void> => {
    const resp = await axiosClient.get(`${BASE}/${surveyId}/export`, {
      params: { format: 'pdf' },
      responseType: 'blob',
    });
    triggerDownload(resp.data as Blob, `survey-${surveyId}-report.pdf`, 'application/pdf');
  },
};
