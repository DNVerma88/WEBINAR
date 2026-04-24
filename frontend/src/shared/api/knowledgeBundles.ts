import axiosClient from './axiosClient';
import type {
  KnowledgeBundleDto,
  KnowledgeBundleDetailDto,
  GetBundlesRequest,
  CreateKnowledgeBundleRequest,
  UpdateKnowledgeBundleRequest,
  AddBundleItemRequest,
  PagedList,
} from '../types';

export const knowledgeBundlesApi = {
  getBundles: (params?: GetBundlesRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<KnowledgeBundleDto>>('/knowledge-bundles', { params, signal }).then((r) => r.data),

  getBundleById: (id: string, signal?: AbortSignal) =>
    axiosClient.get<KnowledgeBundleDetailDto>(`/knowledge-bundles/${id}`, { signal }).then((r) => r.data),

  createBundle: (data: CreateKnowledgeBundleRequest) =>
    axiosClient.post<KnowledgeBundleDto>('/knowledge-bundles', data).then((r) => r.data),

  updateBundle: (id: string, data: UpdateKnowledgeBundleRequest) =>
    axiosClient.put<KnowledgeBundleDto>(`/knowledge-bundles/${id}`, data).then((r) => r.data),

  addItem: (id: string, data: AddBundleItemRequest) =>
    axiosClient.post(`/knowledge-bundles/${id}/items`, data).then((r) => r.data),

  removeItem: (id: string, assetId: string) =>
    axiosClient.delete(`/knowledge-bundles/${id}/items/${assetId}`).then((r) => r.data),
};
