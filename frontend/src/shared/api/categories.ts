import axiosClient from './axiosClient';
import type { CategoryDto, CreateCategoryRequest, UpdateCategoryRequest } from '../types';

export const categoriesApi = {
  getCategories: (signal?: AbortSignal) =>
    axiosClient.get<CategoryDto[]>('/categories', { signal }).then((r) => r.data),

  getCategoryById: (id: string, signal?: AbortSignal) =>
    axiosClient.get<CategoryDto>(`/categories/${id}`, { signal }).then((r) => r.data),

  createCategory: (data: CreateCategoryRequest) =>
    axiosClient.post<CategoryDto>('/categories', data).then((r) => r.data),

  updateCategory: (id: string, data: UpdateCategoryRequest) =>
    axiosClient.put<CategoryDto>(`/categories/${id}`, data).then((r) => r.data),

  deleteCategory: (id: string) =>
    axiosClient.delete(`/categories/${id}`).then((r) => r.data),
};
