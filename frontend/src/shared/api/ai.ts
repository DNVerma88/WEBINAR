import axiosClient from './axiosClient';
import type {
  TranscriptSearchRequest,
  TranscriptSearchResultDto,
  LearningPathRecommendationDto,
  KnowledgeGapDto,
  AiSummaryRequest,
  AiSummaryResponse,
} from '../types';

export const aiApi = {
  searchTranscripts: (data: TranscriptSearchRequest) =>
    axiosClient.post<TranscriptSearchResultDto[]>('/ai/transcript-search', data).then((r) => r.data),

  getLearningPathRecommendations: (signal?: AbortSignal) =>
    axiosClient.get<LearningPathRecommendationDto[]>('/ai/learning-path-recommendations', { signal }).then((r) => r.data),

  getKnowledgeGaps: (signal?: AbortSignal) =>
    axiosClient.get<KnowledgeGapDto[]>('/ai/knowledge-gaps', { signal }).then((r) => r.data),

  generateSummary: (data: AiSummaryRequest) =>
    axiosClient.post<AiSummaryResponse>('/ai/generate-summary', data).then((r) => r.data),
};
