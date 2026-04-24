import axiosClient from './axiosClient';
import type {
  ContentFlagDto,
  UserSuspensionDto,
  FlagContentRequest,
  GetContentFlagsRequest,
  ReviewFlagRequest,
  SuspendUserRequest,
  LiftSuspensionRequest,
  BulkSessionStatusRequest,
  PagedList,
} from '../types';

export const moderationApi = {
  flagContent: (data: FlagContentRequest) =>
    axiosClient.post<ContentFlagDto>('/moderation/flags', data).then((r) => r.data),

  getContentFlags: (params?: GetContentFlagsRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<ContentFlagDto>>('/moderation/flags', { params, signal }).then((r) => r.data),

  reviewFlag: (flagId: string, data: ReviewFlagRequest) =>
    axiosClient.put<ContentFlagDto>(`/moderation/flags/${flagId}/review`, data).then((r) => r.data),

  suspendUser: (data: SuspendUserRequest) =>
    axiosClient.post<UserSuspensionDto>(`/moderation/users/${data.userId}/suspend`, data).then((r) => r.data),

  liftSuspension: (suspensionId: string, data: LiftSuspensionRequest) =>
    axiosClient.put<UserSuspensionDto>(`/moderation/suspensions/${suspensionId}/lift`, data).then((r) => r.data),

  getActiveSuspensions: (params?: { pageNumber?: number; pageSize?: number }, signal?: AbortSignal) =>
    axiosClient.get<PagedList<UserSuspensionDto>>('/moderation/suspensions', { params, signal }).then((r) => r.data),

  bulkUpdateSessionStatus: (data: BulkSessionStatusRequest) =>
    axiosClient.post('/moderation/sessions/bulk-status', data).then((r) => r.data),
};
