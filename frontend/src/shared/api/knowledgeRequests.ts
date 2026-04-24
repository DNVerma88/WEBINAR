import axiosClient from './axiosClient';
import type {
  KnowledgeRequestDto,
  CreateKnowledgeRequestRequest,
  CloseKnowledgeRequestRequest,
  AddressKnowledgeRequestRequest,
  GetKnowledgeRequestsRequest,
  PagedList,
} from '../types';

export const knowledgeRequestsApi = {
  getKnowledgeRequests: (params?: GetKnowledgeRequestsRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<KnowledgeRequestDto>>('/knowledge-requests', { params, signal }).then((r) => r.data),

  getKnowledgeRequestById: (id: string, signal?: AbortSignal) =>
    axiosClient.get<KnowledgeRequestDto>(`/knowledge-requests/${id}`, { signal }).then((r) => r.data),

  createKnowledgeRequest: (data: CreateKnowledgeRequestRequest) =>
    axiosClient.post<KnowledgeRequestDto>('/knowledge-requests', data).then((r) => r.data),

  upvoteKnowledgeRequest: (id: string) =>
    axiosClient.post(`/knowledge-requests/${id}/upvote`).then((r) => r.data),

  claimKnowledgeRequest: (id: string) =>
    axiosClient.post<KnowledgeRequestDto>(`/knowledge-requests/${id}/claim`).then((r) => r.data),

  closeKnowledgeRequest: (id: string, data: CloseKnowledgeRequestRequest) =>
    axiosClient.post<KnowledgeRequestDto>(`/knowledge-requests/${id}/close`, data).then((r) => r.data),

  addressKnowledgeRequest: (id: string, data: AddressKnowledgeRequestRequest) =>
    axiosClient.post<KnowledgeRequestDto>(`/knowledge-requests/${id}/address`, data).then((r) => r.data),
};

