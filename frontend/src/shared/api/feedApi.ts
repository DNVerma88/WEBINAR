import axiosClient from './axiosClient';
import type { FeedPostDto, FeedRequest, PostSeriesDto, CreateSeriesRequest, UpdateSeriesRequest, UserSummaryDto, ContentReportDto } from '../types/feed';
import type { CommunityPostSummaryDto, PagedList } from '../types';

export const feedApi = {
  getPersonalizedFeed: (params?: FeedRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<FeedPostDto>>('/feed', { params, signal }).then((r) => r.data),

  getLatest: (params?: FeedRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<FeedPostDto>>('/feed/latest', { params, signal }).then((r) => r.data),

  getTrending: (pageNumber = 1, pageSize = 20, signal?: AbortSignal) =>
    axiosClient.get<PagedList<FeedPostDto>>('/feed/trending', { params: { pageNumber, pageSize }, signal }).then((r) => r.data),

  getBookmarks: (pageNumber = 1, pageSize = 20, signal?: AbortSignal) =>
    axiosClient.get<PagedList<CommunityPostSummaryDto>>('/feed/bookmarks', { params: { pageNumber, pageSize }, signal }).then((r) => r.data),
};

export const userFollowApi = {
  toggleFollow: (userId: string) =>
    axiosClient.post<{ isFollowing: boolean }>(`/users/${userId}/follow`).then((r) => r.data),

  getFollowers: (userId: string, pageNumber = 1, pageSize = 20, signal?: AbortSignal) =>
    axiosClient.get<PagedList<UserSummaryDto>>(`/users/${userId}/followers`, { params: { pageNumber, pageSize }, signal }).then((r) => r.data),

  getFollowing: (userId: string, pageNumber = 1, pageSize = 20, signal?: AbortSignal) =>
    axiosClient.get<PagedList<UserSummaryDto>>(`/users/${userId}/following`, { params: { pageNumber, pageSize }, signal }).then((r) => r.data),
};

export const postSeriesApi = {
  getSeries: (communityId: string, pageNumber = 1, pageSize = 20, signal?: AbortSignal) =>
    axiosClient.get<PagedList<PostSeriesDto>>(`/communities/${communityId}/series`, { params: { pageNumber, pageSize }, signal }).then((r) => r.data),

  getSeriesById: (communityId: string, seriesId: string, signal?: AbortSignal) =>
    axiosClient.get<PostSeriesDto>(`/communities/${communityId}/series/${seriesId}`, { signal }).then((r) => r.data),

  createSeries: (communityId: string, data: CreateSeriesRequest) =>
    axiosClient.post<PostSeriesDto>(`/communities/${communityId}/series`, data).then((r) => r.data),

  updateSeries: (communityId: string, seriesId: string, data: UpdateSeriesRequest) =>
    axiosClient.put<PostSeriesDto>(`/communities/${communityId}/series/${seriesId}`, data).then((r) => r.data),

  deleteSeries: (communityId: string, seriesId: string) =>
    axiosClient.delete(`/communities/${communityId}/series/${seriesId}`).then((r) => r.data),

  addPost: (communityId: string, seriesId: string, postId: string, order = 0) =>
    axiosClient.post(`/communities/${communityId}/series/${seriesId}/posts/${postId}`, null, { params: { order } }).then((r) => r.data),

  removePost: (communityId: string, seriesId: string, postId: string) =>
    axiosClient.delete(`/communities/${communityId}/series/${seriesId}/posts/${postId}`).then((r) => r.data),
};

export const communityModerationApi = {
  getOpenReports: (pageNumber = 1, pageSize = 20, signal?: AbortSignal) =>
    axiosClient.get<PagedList<ContentReportDto>>('/moderation/community/reports', { params: { pageNumber, pageSize }, signal }).then((r) => r.data),

  resolveReport: (reportId: string, data: { moderatorNote?: string }) =>
    axiosClient.post(`/moderation/community/reports/${reportId}/resolve`, data).then((r) => r.data),

  dismissReport: (reportId: string) =>
    axiosClient.post(`/moderation/community/reports/${reportId}/dismiss`).then((r) => r.data),
};
