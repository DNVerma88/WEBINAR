import axiosClient from './axiosClient';
import type { TagDto, CreateTagRequest, GetTagsRequest, PagedList, CommunityPostSummaryDto, GetPostsRequest } from '../types';

export const tagsApi = {
  getTags: (params?: GetTagsRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<TagDto>>('/tags', { params, signal }).then((r) => r.data),

  createTag: (data: CreateTagRequest) =>
    axiosClient.post<TagDto>('/tags', data).then((r) => r.data),

  deleteTag: (id: string) =>
    axiosClient.delete(`/tags/${id}`).then((r) => r.data),

  getPostsByTag: (slug: string, params?: GetPostsRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<CommunityPostSummaryDto>>(`/tags/${slug}/posts`, { params, signal }).then((r) => r.data),

  toggleFollowTag: (slug: string) =>
    axiosClient.post<{ isFollowing: boolean }>(`/tags/${slug}/follow`).then((r) => r.data),
};
