import axiosClient from './axiosClient';
import type {
  UserDto,
  UpdateUserRequest,
  CreateUserRequest,
  AdminUpdateUserRequest,
  GetUsersRequest,
  ContributorProfileDto,
  UpdateContributorProfileRequest,
  PagedList,
} from '../types';

export const usersApi = {
  getUsers: (params: GetUsersRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<UserDto>>('/users', { params, signal }).then((r) => r.data),

  getUserById: (id: string, signal?: AbortSignal) =>
    axiosClient.get<UserDto>(`/users/${id}`, { signal }).then((r) => r.data),

  createUser: (data: CreateUserRequest) =>
    axiosClient.post<UserDto>('/users', data).then((r) => r.data),

  updateUser: (id: string, data: UpdateUserRequest) =>
    axiosClient.put<UserDto>(`/users/${id}`, data).then((r) => r.data),

  adminUpdateUser: (id: string, data: AdminUpdateUserRequest) =>
    axiosClient.put<UserDto>(`/users/${id}/admin`, data).then((r) => r.data),

  deactivateUser: (id: string) =>
    axiosClient.delete(`/users/${id}`).then((r) => r.data),

  followUser: (id: string) =>
    axiosClient.post(`/users/${id}/follow`).then((r) => r.data),

  unfollowUser: (id: string) =>
    axiosClient.delete(`/users/${id}/follow`).then((r) => r.data),

  getContributorProfile: (userId: string, signal?: AbortSignal) =>
    axiosClient.get<ContributorProfileDto>(`/users/${userId}/contributor-profile`, { signal }).then((r) => r.data),

  updateContributorProfile: (userId: string, data: UpdateContributorProfileRequest) =>
    axiosClient.put<ContributorProfileDto>(`/users/${userId}/contributor-profile`, data).then((r) => r.data),
};
