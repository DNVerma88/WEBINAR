import axiosClient from './axiosClient';
import type {
  LearningPathDto,
  LearningPathDetailDto,
  EnrolmentProgressDto,
  LearningPathCertificateDto,
  UserEnrolmentDto,
  GetLearningPathsRequest,
  CreateLearningPathRequest,
  UpdateLearningPathRequest,
  AddLearningPathItemRequest,
  LearningPathItemDto,
  PagedList,
  LearningPathCohortDto,
  CreateLearningPathCohortRequest,
  UpdateLearningPathCohortRequest,
} from '../types';

export const learningPathsApi = {
  getPaths: (params?: GetLearningPathsRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<LearningPathDto>>('/learning-paths', { params, signal }).then((r) => r.data),

  getPathById: (id: string, signal?: AbortSignal) =>
    axiosClient.get<LearningPathDetailDto>(`/learning-paths/${id}`, { signal }).then((r) => r.data),

  createPath: (data: CreateLearningPathRequest) =>
    axiosClient.post<LearningPathDto>('/learning-paths', data).then((r) => r.data),

  updatePath: (id: string, data: UpdateLearningPathRequest) =>
    axiosClient.put<LearningPathDto>(`/learning-paths/${id}`, data).then((r) => r.data),

  enrol: (id: string) =>
    axiosClient.post(`/learning-paths/${id}/enrol`).then((r) => r.data),

  unenrol: (id: string) =>
    axiosClient.delete(`/learning-paths/${id}/enrol`).then((r) => r.data),

  getProgress: (id: string, signal?: AbortSignal) =>
    axiosClient.get<EnrolmentProgressDto>(`/learning-paths/${id}/progress`, { signal }).then((r) => r.data),

  getCertificate: (id: string, signal?: AbortSignal) =>
    axiosClient.get<LearningPathCertificateDto>(`/learning-paths/${id}/certificate`, { signal }).then((r) => r.data),

  getUserEnrolments: (userId: string, signal?: AbortSignal) =>
    axiosClient.get<UserEnrolmentDto[]>(`/users/${userId}/enrolments`, { signal }).then((r) => r.data),

  getCohorts: (pathId: string, signal?: AbortSignal) =>
    axiosClient.get<LearningPathCohortDto[]>(`/learning-paths/${pathId}/cohorts`, { signal }).then((r) => r.data),

  createCohort: (pathId: string, data: CreateLearningPathCohortRequest) =>
    axiosClient.post<LearningPathCohortDto>(`/learning-paths/${pathId}/cohorts`, data).then((r) => r.data),

  updateCohort: (pathId: string, cohortId: string, data: UpdateLearningPathCohortRequest) =>
    axiosClient.put<LearningPathCohortDto>(`/learning-paths/${pathId}/cohorts/${cohortId}`, data).then((r) => r.data),

  deleteCohort: (pathId: string, cohortId: string) =>
    axiosClient.delete(`/learning-paths/${pathId}/cohorts/${cohortId}`).then((r) => r.data),

  addItem: (pathId: string, data: AddLearningPathItemRequest) =>
    axiosClient.post<LearningPathItemDto>(`/learning-paths/${pathId}/items`, data).then((r) => r.data),

  removeItem: (pathId: string, itemId: string) =>
    axiosClient.delete(`/learning-paths/${pathId}/items/${itemId}`).then((r) => r.data),
};
