import axiosClient from './axiosClient';
import type { NotificationDto, GetNotificationsRequest, PagedList } from '../types';

export const notificationsApi = {
  getNotifications: (params?: GetNotificationsRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<NotificationDto>>('/notifications', { params, signal }).then((r) => r.data),

  markAsRead: (id: string) =>
    axiosClient.put(`/notifications/${id}/read`).then((r) => r.data),

  markAllAsRead: () =>
    axiosClient.put('/notifications/read-all').then((r) => r.data),
};
