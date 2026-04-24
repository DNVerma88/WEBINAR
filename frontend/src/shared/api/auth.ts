import axiosClient from './axiosClient';
import type {
  LoginRequest,
  LoginResponse,
  RefreshTokenRequest,
  RefreshTokenResponse,
  RegisterRequest,
  RegisterResponse,
} from '../types';

export const authApi = {
  register: (data: RegisterRequest) =>
    axiosClient.post<RegisterResponse>('/auth/register', data).then((r) => r.data),

  login: (data: LoginRequest) =>
    axiosClient.post<LoginResponse>('/auth/login', data).then((r) => r.data),

  refresh: (data: RefreshTokenRequest) =>
    axiosClient.post<RefreshTokenResponse>('/auth/refresh', data).then((r) => r.data),

  logout: () =>
    axiosClient.post('/auth/logout').then((r) => r.data),
};
