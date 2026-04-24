// ── AI types ──────────────────────────────────────────────────────────────────

export interface TranscriptSearchRequest {
  query: string;
  maxResults?: number;
}

export interface TranscriptSearchResultDto {
  sessionId: string;
  sessionTitle: string;
  snippet: string;
  relevanceScore: number;
}

export interface LearningPathRecommendationDto {
  learningPathId: string;
  title: string;
  description: string;
  relevanceScore: number;
  reason: string;
}

export interface KnowledgeGapDto {
  topic: string;
  gapScore: number;
  suggestedContent: string[];
}

export interface AiSummaryRequest {
  contentType: string;
  contentId: string;
}

export interface AiSummaryResponse {
  summary: string;
  keyPoints: string[];
  tags: string[];
}
