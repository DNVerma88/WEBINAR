import axiosClient from './axiosClient';
import type {
  SessionDto,
  CreateSessionRequest,
  UpdateSessionRequest,
  GetSessionsRequest,
  SessionMaterialDto,
  AddSessionMaterialRequest,
  SubmitSessionRatingRequest,
  SessionRatingDto,
  SessionRatingSummaryDto,
  SessionChapterDto,
  AddChapterRequest,
  SessionQuizDto,
  CreateQuizRequest,
  UpdateQuizRequest,
  SubmitQuizAttemptRequest,
  QuizAttemptResultDto,
  UserQuizAttemptDto,
  AfterActionReviewDto,
  CreateAarRequest,
  UpdateAarRequest,
  EndorseSkillRequest,
  SkillEndorsementDto,
  CommentDto,
  CreateCommentRequest,
  LikeToggleResult,
  PagedList,
} from '../types';

export const sessionsApi = {
  getSessions: (params?: GetSessionsRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<SessionDto>>('/sessions', { params, signal }).then((r) => r.data),

  getSessionById: (id: string, signal?: AbortSignal) =>
    axiosClient.get<SessionDto>(`/sessions/${id}`, { signal }).then((r) => r.data),

  createSession: (data: CreateSessionRequest) =>
    axiosClient.post<SessionDto>('/sessions', data).then((r) => r.data),

  updateSession: (id: string, data: UpdateSessionRequest) =>
    axiosClient.put<SessionDto>(`/sessions/${id}`, data).then((r) => r.data),

  cancelSession: (id: string) =>
    axiosClient.post<SessionDto>(`/sessions/${id}/cancel`).then((r) => r.data),

  completeSession: (id: string) =>
    axiosClient.post<SessionDto>(`/sessions/${id}/complete`).then((r) => r.data),

  registerForSession: (id: string) =>
    axiosClient.post(`/sessions/${id}/register`).then((r) => r.data),

  cancelRegistration: (id: string) =>
    axiosClient.delete(`/sessions/${id}/register`).then((r) => r.data),

  getMaterials: (id: string, signal?: AbortSignal) =>
    axiosClient.get<SessionMaterialDto[]>(`/sessions/${id}/materials`, { signal }).then((r) => r.data),

  addMaterial: (id: string, data: AddSessionMaterialRequest) =>
    axiosClient.post<SessionMaterialDto>(`/sessions/${id}/materials`, data).then((r) => r.data),

  getRatingsSummary: (id: string, signal?: AbortSignal) =>
    axiosClient.get<SessionRatingSummaryDto>(`/sessions/${id}/ratings`, { signal }).then((r) => r.data),

  submitRating: (id: string, data: SubmitSessionRatingRequest) =>
    axiosClient.post<SessionRatingDto>(`/sessions/${id}/ratings`, data).then((r) => r.data),

  // Chapters
  getChapters: (id: string, signal?: AbortSignal) =>
    axiosClient.get<SessionChapterDto[]>(`/sessions/${id}/chapters`, { signal }).then((r) => r.data),

  addChapter: (id: string, data: AddChapterRequest) =>
    axiosClient.post<SessionChapterDto>(`/sessions/${id}/chapters`, data).then((r) => r.data),

  deleteChapter: (id: string, chapterId: string) =>
    axiosClient.delete(`/sessions/${id}/chapters/${chapterId}`).then((r) => r.data),

  // Quiz
  getQuiz: (id: string, signal?: AbortSignal) =>
    axiosClient.get<SessionQuizDto>(`/sessions/${id}/quiz`, { signal }).then((r) => r.data),

  createQuiz: (id: string, data: CreateQuizRequest) =>
    axiosClient.post<SessionQuizDto>(`/sessions/${id}/quiz`, data).then((r) => r.data),

  updateQuiz: (id: string, data: UpdateQuizRequest) =>
    axiosClient.put<SessionQuizDto>(`/sessions/${id}/quiz`, data).then((r) => r.data),

  submitQuizAttempt: (id: string, data: SubmitQuizAttemptRequest) =>
    axiosClient.post<QuizAttemptResultDto>(`/sessions/${id}/quiz/attempts`, data).then((r) => r.data),

  getMyQuizAttempts: (id: string, signal?: AbortSignal) =>
    axiosClient.get<UserQuizAttemptDto[]>(`/sessions/${id}/quiz/attempts`, { signal }).then((r) => r.data),

  // After-Action Review
  getAar: (id: string, signal?: AbortSignal) =>
    axiosClient.get<AfterActionReviewDto>(`/sessions/${id}/aar`, { signal }).then((r) => r.data),

  createAar: (id: string, data: CreateAarRequest) =>
    axiosClient.post<AfterActionReviewDto>(`/sessions/${id}/aar`, data).then((r) => r.data),

  updateAar: (id: string, data: UpdateAarRequest) =>
    axiosClient.put<AfterActionReviewDto>(`/sessions/${id}/aar`, data).then((r) => r.data),

  // Endorsements
  endorseSkill: (id: string, data: EndorseSkillRequest) =>
    axiosClient.post<SkillEndorsementDto>(`/sessions/${id}/endorsements`, data).then((r) => r.data),

  // Comments
  getComments: (id: string, signal?: AbortSignal) =>
    axiosClient.get<CommentDto[]>(`/sessions/${id}/comments`, { signal }).then((r) => r.data),

  addComment: (id: string, data: CreateCommentRequest) =>
    axiosClient.post<CommentDto>(`/sessions/${id}/comments`, data).then((r) => r.data),

  deleteComment: (id: string, commentId: string) =>
    axiosClient.delete(`/sessions/${id}/comments/${commentId}`).then((r) => r.data),

  likeSession: (id: string) =>
    axiosClient.post<LikeToggleResult>(`/sessions/${id}/like`).then((r) => r.data),

  likeComment: (commentId: string) =>
    axiosClient.post<LikeToggleResult>(`/comments/${commentId}/like`).then((r) => r.data),
};
