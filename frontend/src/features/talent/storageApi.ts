import axiosClient from '../../shared/api/axiosClient';
import type { StorageProvider, StorageFileItem, StorageFileRef } from './types';

export const storageApi = {
  getProviders: () =>
    axiosClient.get<StorageProvider[]>('/talent/storage/providers').then((r) => r.data),
  listFiles: (provider: string, path: string) =>
    axiosClient
      .get<StorageFileItem[]>('/talent/storage/list', { params: { provider, path } })
      .then((r) => r.data),
  verifyReference: (ref: StorageFileRef) =>
    axiosClient
      .post<{ accessible: boolean }>('/talent/storage/verify-reference', ref)
      .then((r) => r.data),
};
