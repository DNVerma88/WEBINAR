export interface CommunityDto {
  id: string;
  name: string;
  description?: string;
  memberCount: number;
  isMember: boolean;
  createdDate: string;
}

export interface CreateCommunityRequest {
  name: string;
  description?: string;
}

export interface UpdateCommunityRequest {
  name: string;
  description?: string;
}

export interface GetCommunitiesRequest {
  search?: string;
  page?: number;
  pageSize?: number;
}
