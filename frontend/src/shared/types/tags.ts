export interface TagDto {
  id: string;
  name: string;
  slug: string;
  description?: string;
  isActive: boolean;
  isOfficial: boolean;
  postCount: number;
  usageCount: number;
  isFollowedByCurrentUser: boolean;
}

export interface CreateTagRequest {
  name: string;
}

export interface GetTagsRequest {
  search?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}
