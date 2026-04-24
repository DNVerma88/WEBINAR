// ── Enums ─────────────────────────────────────────────────────────────────────

export type SurveyStatus = 'Draft' | 'Active' | 'Closed';

export type SurveyQuestionType = 'Text' | 'SingleChoice' | 'MultipleChoice' | 'Rating' | 'YesNo';

export type SurveyInvitationStatus = 'Pending' | 'Sent' | 'Submitted' | 'Expired' | 'Failed';

// ── Core DTOs ────────────────────────────────────────────────────────────────

export interface SurveyDto {
  id: string;
  title: string;
  description?: string;
  welcomeMessage?: string;
  thankYouMessage?: string;
  status: SurveyStatus;
  endsAt?: string;
  isAnonymous: boolean;
  launchedAt?: string;
  closedAt?: string;
  totalInvited: number;
  totalResponded: number;
  questionCount: number;
  questions?: SurveyQuestionDto[];
  createdDate: string;
  recordVersion: number;
}

export interface SurveyQuestionDto {
  id: string;
  surveyId: string;
  questionText: string;
  questionType: SurveyQuestionType;
  options?: string[];
  minRating: number;
  maxRating: number;
  isRequired: boolean;
  orderSequence: number;
}

export interface SurveyInvitationDto {
  id: string;
  surveyId: string;
  userId: string;
  userFullName: string;
  userEmail: string;
  status: SurveyInvitationStatus;
  sentAt?: string;
  expiresAt?: string;
  submittedAt?: string;
  resendCount: number;
}

export interface SurveyResultsDto {
  surveyId: string;
  surveyTitle: string;
  totalInvited: number;
  totalResponded: number;
  responseRatePct: number;
  questionResults: QuestionResultDto[];
}

export interface QuestionResultDto {
  questionId: string;
  questionText: string;
  questionType: SurveyQuestionType;
  totalAnswers: number;
  optionCounts: OptionCountDto[] | null;
  averageRating?: number | null;
  minRating?: number | null;
  maxRating?: number | null;
  textAnswers: string[] | null;
}

export interface OptionCountDto {
  optionValue: string;
  count: number;
  percentage: number;
}

export interface SurveyResponseDto {
  id: string;
  surveyId: string;
  userId?: string;
  userFullName?: string;
  submittedAt: string;
  answers: SurveyAnswerDto[];
}

export interface SurveyAnswerDto {
  questionId: string;
  questionText: string;
  answerText?: string;
  answerOptions?: string[];
  ratingValue?: number;
}

// ── Form DTO (public) ────────────────────────────────────────────────────────

export interface SurveyFormDto {
  surveyId: string;
  title: string;
  description?: string;
  welcomeMessage?: string;
  thankYouMessage?: string;
  questions: SurveyQuestionDto[];
}

// ── Request Types ────────────────────────────────────────────────────────────

export interface CreateSurveyRequest {
  title: string;
  description?: string;
  welcomeMessage?: string;
  thankYouMessage?: string;
  endsAt?: string;
  isAnonymous: boolean;
}

export interface UpdateSurveyRequest {
  title: string;
  description?: string;
  welcomeMessage?: string;
  thankYouMessage?: string;
  endsAt?: string;
  isAnonymous: boolean;
  recordVersion: number;
}

export interface CopySurveyRequest {
  newTitle?: string;
  excludeQuestionIds?: string[];
}

export interface AddSurveyQuestionRequest {
  questionText: string;
  questionType: SurveyQuestionType;
  options?: string[];
  minRating?: number;
  maxRating?: number;
  isRequired: boolean;
}

export interface UpdateSurveyQuestionRequest {
  questionText: string;
  questionType: SurveyQuestionType;
  options?: string[];
  minRating?: number;
  maxRating?: number;
  isRequired: boolean;
  recordVersion: number;
}

export interface ReorderQuestionsRequest {
  orderedIds: string[];
}

export interface SubmitSurveyRequest {
  answers: SurveyAnswerRequest[];
}

export interface SurveyAnswerRequest {
  questionId: string;
  answerText?: string;
  answerOptions?: string[];
  ratingValue?: number;
}

export interface ResendInvitationsRequest {
  userIds: string[];
}

// ── Analytics DTOs ────────────────────────────────────────────────────────────

export interface SurveyAnalyticsSummaryDto {
  surveyId: string;
  surveyTitle: string;
  totalInvited: number;
  totalSubmitted: number;
  responseRatePct: number;
  avgCompletionTimeSeconds: number;
  healthStatus: 'Healthy' | 'AtRisk' | 'LowEngagement';
}

export interface SurveyQuestionAnalyticsDto {
  questionId: string;
  questionText: string;
  questionType: SurveyQuestionType;
  totalAnswers: number;
  optionStats: OptionStatDto[] | null;
  averageRating?: number | null;
  minRating?: number | null;
  maxRating?: number | null;
  textAnswers: string[] | null;
}

export interface OptionStatDto {
  optionValue: string;
  count: number;
  percentage: number;
}

export interface DepartmentRowDto {
  department: string;
  averageScore: number;
  responseCount: number;
}

export interface SurveyDepartmentBreakdownDto {
  questionId: string;
  questionText: string;
  rows: DepartmentRowDto[];
}

export interface SurveyNpsReportDto {
  surveyId: string;
  surveyTitle: string;
  promoters: number;
  passives: number;
  detractors: number;
  npsScore: number;
  promoterPct: number;
  passivePct: number;
  detractorPct: number;
}

export interface NpsTrendPointDto {
  surveyId: string;
  surveyTitle: string;
  launchedAt: string;
  npsScore: number;
}

export interface SurveyNpsTrendDto {
  dataPoints: NpsTrendPointDto[];
}

export interface SurveyParticipationFunnelDto {
  totalInvited: number;
  totalEmailsSent: number;
  totalTokensAccessed: number;
  totalSubmitted: number;
  submissionRatePct: number;
  startToSubmitRatePct: number;
}

export interface SurveyHeatmapDto {
  questionTexts: string[];
  departments: string[];
  matrix: number[][];
}

export interface SharedQuestionCompDto {
  questionText: string;
  surveyStats: SurveyQuestionAnalyticsDto[];
}

export interface SurveyCompSummaryDto {
  surveyId: string;
  title: string;
  launchedAt?: string;
  responseRatePct: number;
}

export interface SurveyComparisonDto {
  surveys: SurveyCompSummaryDto[];
  sharedQuestions: SharedQuestionCompDto[];
}

export interface QuestionStatsFilters {
  department?: string;
  role?: string;
  fromDate?: string;
  toDate?: string;
}
