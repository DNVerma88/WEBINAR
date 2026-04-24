export enum SessionFormat {
  Webinar = 'Webinar',
  Workshop = 'Workshop',
  Demo = 'Demo',
  PanelDiscussion = 'PanelDiscussion',
  KnowledgeSharingTalk = 'KnowledgeSharingTalk',
  OfficeHours = 'OfficeHours',
  HackSession = 'HackSession',
  LearningSeriesEpisode = 'LearningSeriesEpisode',
  KnowledgeHarvestSession = 'KnowledgeHarvestSession',
  ExpertInterview = 'ExpertInterview',
}

export enum SessionStatus {
  Scheduled = 'Scheduled',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
}

export enum DifficultyLevel {
  Beginner = 'Beginner',
  Intermediate = 'Intermediate',
  Advanced = 'Advanced',
}

export enum MaterialType {
  Slides = 'Slides',
  Document = 'Document',
  DemoLink = 'DemoLink',
  RecordingLink = 'RecordingLink',
  CodeRepository = 'CodeRepository',
  FAQ = 'FAQ',
}

export enum MeetingPlatform {
  Teams = 'Teams',
  Zoom = 'Zoom',
  GoogleMeet = 'GoogleMeet',
  Webex = 'Webex',
  GoToMeeting = 'GoToMeeting',
  BlueJeans = 'BlueJeans',
}

export const SESSION_FORMAT_LABELS: Record<SessionFormat, string> = {
  [SessionFormat.Webinar]: 'Webinar',
  [SessionFormat.Workshop]: 'Workshop',
  [SessionFormat.Demo]: 'Demo',
  [SessionFormat.PanelDiscussion]: 'Panel Discussion',
  [SessionFormat.KnowledgeSharingTalk]: 'Knowledge Sharing Talk',
  [SessionFormat.OfficeHours]: 'Office Hours',
  [SessionFormat.HackSession]: 'Hack Session',
  [SessionFormat.LearningSeriesEpisode]: 'Learning Series Episode',
  [SessionFormat.KnowledgeHarvestSession]: 'Knowledge Harvest Session',
  [SessionFormat.ExpertInterview]: 'Expert Interview',
};

export const SESSION_STATUS_LABELS: Record<SessionStatus, string> = {
  [SessionStatus.Scheduled]: 'Scheduled',
  [SessionStatus.InProgress]: 'In Progress',
  [SessionStatus.Completed]: 'Completed',
  [SessionStatus.Cancelled]: 'Cancelled',
};

export const DIFFICULTY_LEVEL_LABELS: Record<DifficultyLevel, string> = {
  [DifficultyLevel.Beginner]: 'Beginner',
  [DifficultyLevel.Intermediate]: 'Intermediate',
  [DifficultyLevel.Advanced]: 'Advanced',
};

export const MEETING_PLATFORM_LABELS: Record<MeetingPlatform, string> = {
  [MeetingPlatform.Teams]: 'Teams',
  [MeetingPlatform.Zoom]: 'Zoom',
  [MeetingPlatform.GoogleMeet]: 'Google Meet',
  [MeetingPlatform.Webex]: 'Webex',
  [MeetingPlatform.GoToMeeting]: 'Go To Meeting',
  [MeetingPlatform.BlueJeans]: 'Blue Jeans',
};

export interface SessionDto {
  id: string;
  proposalId: string;
  speakerId: string;
  speakerName: string;
  speakerPhotoUrl?: string;
  title: string;
  categoryId: string;
  categoryName: string;
  format: SessionFormat;
  difficultyLevel: DifficultyLevel;
  status: SessionStatus;
  scheduledAt: string;
  durationMinutes: number;
  meetingLink: string;
  meetingPlatform: MeetingPlatform;
  participantLimit?: number;
  registeredCount: number;
  isPublic: boolean;
  recordingUrl?: string;
  description?: string;
  tags: string[];
  recordVersion: number;
  // set by service layer
  isRegistered?: boolean;
  waitlistCount?: number;
}

export interface CreateSessionRequest {
  proposalId: string;
  speakerId?: string;
  scheduledAt: string;
  durationMinutes: number;
  meetingLink: string;
  meetingPlatform: MeetingPlatform;
  participantLimit?: number;
  isPublic?: boolean;
  tagIds?: string[];
}

export interface UpdateSessionRequest {
  scheduledAt: string;
  durationMinutes: number;
  meetingLink: string;
  meetingPlatform: MeetingPlatform;
  participantLimit?: number;
  isPublic?: boolean;
  recordingUrl?: string;
  speakerId?: string;
  tagIds?: string[];
  recordVersion: number;
}

export interface GetSessionsRequest {
  searchTerm?: string;
  categoryId?: string;
  format?: SessionFormat;
  difficultyLevel?: DifficultyLevel;
  status?: SessionStatus;
  speakerId?: string;
  tagId?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface SessionMaterialDto {
  id: string;
  sessionId?: string;
  proposalId?: string;
  materialType: MaterialType;
  title: string;
  url: string;
  fileSizeBytes?: number;
}

export interface AddSessionMaterialRequest {
  materialType: MaterialType;
  title: string;
  url: string;
  fileSizeBytes?: number;
}

export interface SubmitSessionRatingRequest {
  sessionScore: number;
  speakerScore: number;
  feedbackText?: string;
  nextSessionSuggestion?: string;
}

export interface SessionRatingDto {
  id: string;
  sessionId: string;
  raterId: string;
  raterName: string;
  sessionScore: number;
  speakerScore: number;
  feedbackText?: string;
  nextSessionSuggestion?: string;
  createdDate: string;
}

export interface SessionRatingSummaryDto {
  averageSessionScore: number;
  averageSpeakerScore: number;
  totalRatings: number;
}

// ─── Session Chapters ───────────────────────────────────────────────────────
export interface SessionChapterDto {
  id: string;
  sessionId: string;
  title: string;
  timestampSeconds: number;
  orderSequence: number;
}

export interface AddChapterRequest {
  title: string;
  timestampSeconds: number;
  orderSequence: number;
}

// ─── Quiz ────────────────────────────────────────────────────────────────────
export enum QuizQuestionType {
  MultipleChoice = 'MultipleChoice',
  TrueFalse = 'TrueFalse',
  ShortText = 'ShortText',
}

export interface QuizQuestionDto {
  id: string;
  questionText: string;
  questionType: QuizQuestionType;
  options?: string[];
  correctAnswer?: string;
  orderSequence: number;
  points: number;
}

export interface SessionQuizDto {
  id: string;
  sessionId: string;
  title: string;
  description?: string;
  passingThresholdPercent: number;
  allowRetry: boolean;
  maxAttempts: number;
  isActive: boolean;
  questions: QuizQuestionDto[];
}

export interface QuizAnswerRequest {
  questionId: string;
  answer: string;
}

export interface SubmitQuizAttemptRequest {
  answers: QuizAnswerRequest[];
}

export interface QuizAttemptResultDto {
  attemptNumber: number;
  score?: number;
  isPassed?: boolean;
  submittedAt: string;
  xpAwarded: boolean;
}

export interface UserQuizAttemptDto {
  id: string;
  attemptNumber: number;
  score?: number;
  isPassed?: boolean;
  submittedAt: string;
  gradedAt?: string;
}

export interface CreateQuizRequest {
  title: string;
  description?: string;
  passingThresholdPercent: number;
  allowRetry: boolean;
  maxAttempts: number;
  questions: CreateQuizQuestionRequest[];
}

export interface UpdateQuizRequest {
  title: string;
  description?: string;
  passingThresholdPercent: number;
  allowRetry: boolean;
  maxAttempts: number;
  isActive: boolean;
  questions: CreateQuizQuestionRequest[];
}

export interface CreateQuizQuestionRequest {
  questionText: string;
  questionType: QuizQuestionType;
  options?: string[];
  correctAnswer?: string;
  orderSequence: number;
  points: number;
}

// ─── After-Action Review ─────────────────────────────────────────────────────
export interface AfterActionReviewDto {
  id: string;
  sessionId: string;
  authorId: string;
  authorName: string;
  whatWasPlanned: string;
  whatHappened: string;
  whatWentWell: string;
  whatToImprove: string;
  keyLessonsLearned: string;
  isPublished: boolean;
  createdDate: string;
}

export interface CreateAarRequest {
  whatWasPlanned: string;
  whatHappened: string;
  whatWentWell: string;
  whatToImprove: string;
  keyLessonsLearned: string;
  isPublished: boolean;
}

export interface UpdateAarRequest {
  whatWasPlanned: string;
  whatHappened: string;
  whatWentWell: string;
  whatToImprove: string;
  keyLessonsLearned: string;
  isPublished: boolean;
}

// ─── Endorsements ─────────────────────────────────────────────────────────────
export interface EndorseSkillRequest {
  endorseeId: string;
  tagId: string;
}

// ─── Comments / Likes ─────────────────────────────────────────────────────────
export interface CommentDto {
  id: string;
  sessionId?: string;
  knowledgeAssetId?: string;
  authorId: string;
  authorName: string;
  content: string;
  parentCommentId?: string;
  likeCount: number;
  hasLiked: boolean;
  isDeleted: boolean;
  createdDate: string;
  replies: CommentDto[];
}

export interface CreateCommentRequest {
  content: string;
  parentCommentId?: string;
}

export interface LikeToggleResult {
  liked: boolean;
  likeCount: number;
}
