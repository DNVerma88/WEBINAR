export interface CategoryDto {
  id: string;
  name: string;
  description?: string;
  iconName?: string;
  sortOrder: number;
  isActive: boolean;
  recordVersion: number;
}

export interface CreateCategoryRequest {
  name: string;
  description?: string;
  iconName?: string;
  sortOrder?: number;
}

export interface UpdateCategoryRequest {
  name: string;
  description?: string;
  iconName?: string;
  sortOrder?: number;
  isActive?: boolean;
  recordVersion: number;
}
