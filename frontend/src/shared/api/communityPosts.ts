import axiosClient from './axiosClient';
import type {
  CommunityPostSummaryDto,
  CommunityPostDetailDto,
  PostReactionResultDto,
  PostCommentDto,
  PostBookmarkToggleResult,
  GetPostsRequest,
  CreatePostRequest,
  UpdatePostRequest,
  AddCommentRequest,
  ToggleReactionRequest,
  PagedList,
} from '../types';
import type { ReportContentRequest } from '../types/feed';

const base = (communityId: string) => `/communities/${communityId}/posts`;

export const communityPostsApi = {
  getPosts: (communityId: string, params?: GetPostsRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<CommunityPostSummaryDto>>(base(communityId), { params, signal }).then((r) => r.data),

  getPost: (communityId: string, postId: string, signal?: AbortSignal) =>
    axiosClient.get<CommunityPostDetailDto>(`${base(communityId)}/${postId}`, { signal }).then((r) => r.data),

  createPost: (communityId: string, data: CreatePostRequest) =>
    axiosClient.post<CommunityPostDetailDto>(base(communityId), data).then((r) => r.data),

  updatePost: (communityId: string, postId: string, data: UpdatePostRequest) =>
    axiosClient.put<CommunityPostDetailDto>(`${base(communityId)}/${postId}`, data).then((r) => r.data),

  deletePost: (communityId: string, postId: string) =>
    axiosClient.delete(`${base(communityId)}/${postId}`).then((r) => r.data),

  togglePin: (communityId: string, postId: string) =>
    axiosClient.post(`${base(communityId)}/${postId}/pin`).then((r) => r.data),

  saveDraft: (communityId: string, postId: string, data: { title?: string; contentMarkdown?: string }) =>
    axiosClient.patch(`${base(communityId)}/${postId}/draft`, data).then((r) => r.data),

  getReactions: (communityId: string, postId: string, signal?: AbortSignal) =>
    axiosClient.get<PostReactionResultDto>(`${base(communityId)}/${postId}/reactions`, { signal }).then((r) => r.data),

  toggleReaction: (communityId: string, postId: string, data: ToggleReactionRequest) =>
    axiosClient.post<PostReactionResultDto>(`${base(communityId)}/${postId}/reactions`, data).then((r) => r.data),

  getComments: (communityId: string, postId: string, pageNumber = 1, pageSize = 20, signal?: AbortSignal) =>
    axiosClient
      .get<PagedList<PostCommentDto>>(`${base(communityId)}/${postId}/comments`, { params: { pageNumber, pageSize }, signal })
      .then((r) => r.data),

  addComment: (communityId: string, postId: string, data: AddCommentRequest) =>
    axiosClient.post<PostCommentDto>(`${base(communityId)}/${postId}/comments`, data).then((r) => r.data),

  deleteComment: (communityId: string, postId: string, commentId: string) =>
    axiosClient.delete(`${base(communityId)}/${postId}/comments/${commentId}`).then((r) => r.data),

  toggleBookmark: (communityId: string, postId: string) =>
    axiosClient.post<PostBookmarkToggleResult>(`${base(communityId)}/${postId}/bookmark`).then((r) => r.data),

  getMyBookmarks: (pageNumber = 1, pageSize = 20, signal?: AbortSignal) =>
    axiosClient
      .get<PagedList<CommunityPostSummaryDto>>('/posts/bookmarks', { params: { pageNumber, pageSize }, signal })
      .then((r) => r.data),

  reportPost: (communityId: string, postId: string, data: ReportContentRequest) =>
    axiosClient.post(`${base(communityId)}/${postId}/report`, data).then((r) => r.data),

  searchPosts: (communityId: string, q: string, pageNumber = 1, pageSize = 20, signal?: AbortSignal) =>
    axiosClient
      .get<PagedList<CommunityPostSummaryDto>>(`${base(communityId)}/search`, { params: { q, pageNumber, pageSize }, signal })
      .then((r) => r.data),
};
