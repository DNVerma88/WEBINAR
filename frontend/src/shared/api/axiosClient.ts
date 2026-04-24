import axios from 'axios';
import type { RefreshTokenResponse } from '../types';

const BASE_URL = '/api';
const ACCESS_TOKEN_KEY = 'kh_access_token';
const REFRESH_TOKEN_KEY = 'kh_refresh_token';

export const tokenStorage = {
  getAccessToken: () => sessionStorage.getItem(ACCESS_TOKEN_KEY),
  setAccessToken: (token: string) => sessionStorage.setItem(ACCESS_TOKEN_KEY, token),
  // F1: refresh token moved to sessionStorage (same lifetime as tab session) to reduce XSS exposure
  getRefreshToken: () => sessionStorage.getItem(REFRESH_TOKEN_KEY),
  setRefreshToken: (token: string) => sessionStorage.setItem(REFRESH_TOKEN_KEY, token),
  clear: () => {
    sessionStorage.removeItem(ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_TOKEN_KEY);
  },
};

const axiosClient = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor — attach Bearer token
axiosClient.interceptors.request.use((config) => {
  const token = tokenStorage.getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Track whether a refresh is already in progress to avoid duplicate calls
let refreshingPromise: Promise<string> | null = null;

// Response interceptor — 401 → refresh → retry once
axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      const refreshToken = tokenStorage.getRefreshToken();

      if (!refreshToken) {
        tokenStorage.clear();
        window.location.href = '/login';
        return Promise.reject(error);
      }

      try {
        if (!refreshingPromise) {
          refreshingPromise = axios
            .post<RefreshTokenResponse>(`${BASE_URL}/auth/refresh`, { refreshToken })
            .then((res) => {
              tokenStorage.setAccessToken(res.data.accessToken);
              tokenStorage.setRefreshToken(res.data.refreshToken);
              return res.data.accessToken;
            })
            .finally(() => {
              refreshingPromise = null;
            });
        }

        const newToken = await refreshingPromise;
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return axiosClient(originalRequest);
      } catch {
        tokenStorage.clear();
        window.location.href = '/login';
        return Promise.reject(error);
      }
    }

    return Promise.reject(error);
  }
);

export default axiosClient;
