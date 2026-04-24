import axiosClient from './axiosClient';
import type {
  KnowledgeAssetDto,
  CreateKnowledgeAssetRequest,
  GetAssetsRequest,
  PagedList,
} from '../types';

export const knowledgeAssetsApi = {
  getAssets: (params?: GetAssetsRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<KnowledgeAssetDto>>('/knowledge-assets', { params, signal }).then((r) => r.data),

  getAssetById: (id: string, signal?: AbortSignal) =>
    axiosClient.get<KnowledgeAssetDto>(`/knowledge-assets/${id}`, { signal }).then((r) => r.data),

  createAsset: (data: CreateKnowledgeAssetRequest) =>
    axiosClient.post<KnowledgeAssetDto>('/knowledge-assets', data).then((r) => r.data),

  deleteAsset: (id: string) =>
    axiosClient.delete(`/knowledge-assets/${id}`).then((r) => r.data),
};
