import axiosClient from './axiosClient';
import type {
  CommunityDto,
  CreateCommunityRequest,
  UpdateCommunityRequest,
  GetCommunitiesRequest,
  WikiPageDto,
  CreateWikiPageRequest,
  UpdateWikiPageRequest,
  PagedList,
} from '../types';

export const communitiesApi = {
  getCommunities: (params?: GetCommunitiesRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<CommunityDto>>('/communities', { params, signal }).then((r) => r.data),

  getCommunityById: (id: string, signal?: AbortSignal) =>
    axiosClient.get<CommunityDto>(`/communities/${id}`, { signal }).then((r) => r.data),

  createCommunity: (data: CreateCommunityRequest) =>
    axiosClient.post<CommunityDto>('/communities', data).then((r) => r.data),

  updateCommunity: (id: string, data: UpdateCommunityRequest) =>
    axiosClient.put<CommunityDto>(`/communities/${id}`, data).then((r) => r.data),

  deleteCommunity: (id: string) =>
    axiosClient.delete(`/communities/${id}`).then((r) => r.data),

  joinCommunity: (id: string) =>
    axiosClient.post(`/communities/${id}/join`).then((r) => r.data),

  leaveCommunity: (id: string) =>
    axiosClient.delete(`/communities/${id}/join`).then((r) => r.data),

  // Wiki
  getWikiPages: (communityId: string, signal?: AbortSignal) =>
    axiosClient.get<WikiPageDto[]>(`/communities/${communityId}/wiki`, { signal }).then((r) => r.data),

  getWikiPage: (communityId: string, pageId: string, signal?: AbortSignal) =>
    axiosClient.get<WikiPageDto>(`/communities/${communityId}/wiki/${pageId}`, { signal }).then((r) => r.data),

  createWikiPage: (communityId: string, data: CreateWikiPageRequest) =>
    axiosClient.post<WikiPageDto>(`/communities/${communityId}/wiki`, data).then((r) => r.data),

  updateWikiPage: (communityId: string, pageId: string, data: UpdateWikiPageRequest) =>
    axiosClient.put<WikiPageDto>(`/communities/${communityId}/wiki/${pageId}`, data).then((r) => r.data),

  deleteWikiPage: (communityId: string, pageId: string) =>
    axiosClient.delete(`/communities/${communityId}/wiki/${pageId}`).then((r) => r.data),
};
