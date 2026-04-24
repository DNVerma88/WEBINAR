import axiosClient from './axiosClient';
import type {
  AnalyticsSummaryResponse,
  KnowledgeGapHeatmapResponse,
  SkillCoverageReportResponse,
  ContentFreshnessReportResponse,
  LearningFunnelResponse,
  CohortCompletionRatesResponse,
  DepartmentEngagementScoreResponse,
  KnowledgeRetentionScoreResponse,
} from '../types';

export const analyticsApi = {
  getSummary: (signal?: AbortSignal) =>
    axiosClient.get<AnalyticsSummaryResponse>('/analytics/summary', { signal }).then((r) => r.data),

  getKnowledgeGapHeatmap: (signal?: AbortSignal) =>
    axiosClient.get<KnowledgeGapHeatmapResponse>('/analytics/knowledge-gap-heatmap', { signal }).then((r) => r.data),

  getSkillCoverage: (signal?: AbortSignal) =>
    axiosClient.get<SkillCoverageReportResponse>('/analytics/skill-coverage', { signal }).then((r) => r.data),

  getContentFreshness: (signal?: AbortSignal) =>
    axiosClient.get<ContentFreshnessReportResponse>('/analytics/content-freshness', { signal }).then((r) => r.data),

  getLearningFunnel: (signal?: AbortSignal) =>
    axiosClient.get<LearningFunnelResponse>('/analytics/learning-funnel', { signal }).then((r) => r.data),

  getCohortCompletionRates: (signal?: AbortSignal) =>
    axiosClient.get<CohortCompletionRatesResponse>('/analytics/cohort-completion', { signal }).then((r) => r.data),

  getDepartmentEngagement: (signal?: AbortSignal) =>
    axiosClient.get<DepartmentEngagementScoreResponse>('/analytics/department-engagement', { signal }).then((r) => r.data),

  getKnowledgeRetention: (signal?: AbortSignal) =>
    axiosClient.get<KnowledgeRetentionScoreResponse>('/analytics/knowledge-retention', { signal }).then((r) => r.data),
};
