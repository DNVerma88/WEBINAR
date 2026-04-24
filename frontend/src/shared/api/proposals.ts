import axiosClient from './axiosClient';
import type {
  SessionProposalDto,
  CreateSessionProposalRequest,
  UpdateSessionProposalRequest,
  ApproveProposalRequest,
  RejectProposalRequest,
  RequestRevisionRequest,
  GetProposalsRequest,
  PagedList,
} from '../types';

export const proposalsApi = {
  getProposals: (params?: GetProposalsRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<SessionProposalDto>>('/session-proposals', { params, signal }).then((r) => r.data),

  getProposalById: (id: string, signal?: AbortSignal) =>
    axiosClient.get<SessionProposalDto>(`/session-proposals/${id}`, { signal }).then((r) => r.data),

  createProposal: (data: CreateSessionProposalRequest) =>
    axiosClient.post<SessionProposalDto>('/session-proposals', data).then((r) => r.data),

  updateProposal: (id: string, data: UpdateSessionProposalRequest) =>
    axiosClient.put<SessionProposalDto>(`/session-proposals/${id}`, data).then((r) => r.data),

  deleteProposal: (id: string) =>
    axiosClient.delete(`/session-proposals/${id}`).then((r) => r.data),

  submitProposal: (id: string) =>
    axiosClient.post(`/session-proposals/${id}/submit`).then((r) => r.data),

  approveProposal: (id: string, data?: ApproveProposalRequest) =>
    axiosClient.post(`/session-proposals/${id}/approve`, data ?? {}).then((r) => r.data),

  rejectProposal: (id: string, data: RejectProposalRequest) =>
    axiosClient.post(`/session-proposals/${id}/reject`, data).then((r) => r.data),

  requestRevision: (id: string, data: RequestRevisionRequest) =>
    axiosClient.post(`/session-proposals/${id}/request-revision`, data).then((r) => r.data),
};
