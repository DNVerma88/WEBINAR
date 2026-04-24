import axiosClient from '../../../shared/api/axiosClient';
import type { PagedResult } from '../../../shared/types';
import type {
  SurveyDto,
  SurveyQuestionDto,
  SurveyInvitationDto,
  SurveyResultsDto,
  SurveyResponseDto,
  CreateSurveyRequest,
  UpdateSurveyRequest,
  CopySurveyRequest,
  AddSurveyQuestionRequest,
  UpdateSurveyQuestionRequest,
  ReorderQuestionsRequest,
  ResendInvitationsRequest,
} from '../types';

const BASE = '/surveys';

export const surveysApi = {
  getSurveys: (params?: Record<string, unknown>) =>
    axiosClient.get<PagedResult<SurveyDto>>(BASE, { params }).then(r => r.data),

  getSurveyById: (id: string) =>
    axiosClient.get<SurveyDto>(`${BASE}/${id}`).then(r => r.data),

  createSurvey: (req: CreateSurveyRequest) =>
    axiosClient.post<SurveyDto>(BASE, req).then(r => r.data),

  updateSurvey: (id: string, req: UpdateSurveyRequest) =>
    axiosClient.put<SurveyDto>(`${BASE}/${id}`, req).then(r => r.data),

  deleteSurvey: (id: string) =>
    axiosClient.delete(`${BASE}/${id}`).then(r => r.data),

  copySurvey: (id: string, req: CopySurveyRequest) =>
    axiosClient.post<SurveyDto>(`${BASE}/${id}/copy`, req).then(r => r.data),

  launchSurvey: (id: string) =>
    axiosClient.post(`${BASE}/${id}/launch`).then(r => r.data),

  closeSurvey: (id: string) =>
    axiosClient.post(`${BASE}/${id}/close`).then(r => r.data),

  getResults: (id: string) =>
    axiosClient.get<SurveyResultsDto>(`${BASE}/${id}/results`).then(r => r.data),

  getResponses: (id: string, params?: Record<string, unknown>) =>
    axiosClient.get<PagedResult<SurveyResponseDto>>(`${BASE}/${id}/responses`, { params }).then(r => r.data),

  // Questions
  addQuestion: (id: string, req: AddSurveyQuestionRequest) =>
    axiosClient.post<SurveyQuestionDto>(`${BASE}/${id}/questions`, req).then(r => r.data),

  updateQuestion: (id: string, qId: string, req: UpdateSurveyQuestionRequest) =>
    axiosClient.put<SurveyQuestionDto>(`${BASE}/${id}/questions/${qId}`, req).then(r => r.data),

  deleteQuestion: (id: string, qId: string) =>
    axiosClient.delete(`${BASE}/${id}/questions/${qId}`).then(r => r.data),

  reorderQuestions: (id: string, req: ReorderQuestionsRequest) =>
    axiosClient.post(`${BASE}/${id}/questions/reorder`, req).then(r => r.data),

  // Invitations
  getInvitations: (id: string, params?: Record<string, unknown>) =>
    axiosClient.get<PagedResult<SurveyInvitationDto>>(`${BASE}/${id}/invitations`, { params }).then(r => r.data),

  resendToUser: (id: string, userId: string) =>
    axiosClient.post(`${BASE}/${id}/invitations/${userId}/resend`).then(r => r.data),

  resendBulk: (id: string, req: ResendInvitationsRequest) =>
    axiosClient.post(`${BASE}/${id}/invitations/resend-bulk`, req).then(r => r.data),

  resendAllPending: (id: string) =>
    axiosClient.post(`${BASE}/${id}/invitations/resend-all-pending`).then(r => r.data),
};
