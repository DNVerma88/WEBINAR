export interface UserDto {
  id: string;
  fullName: string;
  email: string;
  department?: string;
  designation?: string;
  yearsOfExperience?: number;
  location?: string;
  profilePhotoUrl?: string;
  role: number;
  isActive: boolean;
  recordVersion: number;
  // extended fields set by service layer
  followerCount?: number;
  isFollowedByCurrentUser?: boolean;
}

export interface UpdateUserRequest {
  fullName: string;
  department?: string;
  designation?: string;
  yearsOfExperience?: number;
  location?: string;
  profilePhotoUrl?: string;
  recordVersion: number;
}

export interface GetUsersRequest {
  search?: string;
  department?: string;
  role?: number;
  isActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export interface CreateUserRequest {
  fullName: string;
  email: string;
  password: string;
  role: number;
  department?: string;
  designation?: string;
  yearsOfExperience?: number;
  location?: string;
}

export interface AdminUpdateUserRequest {
  fullName: string;
  email: string;
  role: number;
  isActive: boolean;
  department?: string;
  designation?: string;
  yearsOfExperience?: number;
  location?: string;
  recordVersion: number;
}

export interface ContributorProfileDto {
  id: string;
  userId: string;
  areasOfExpertise?: string;
  technologiesKnown?: string;
  bio?: string;
  averageRating: number;
  totalSessionsDelivered: number;
  followerCount: number;
  endorsementScore: number;
  isKnowledgeBroker: boolean;
  availableForMentoring: boolean;
  recordVersion: number;
}

export interface UpdateContributorProfileRequest {
  areasOfExpertise?: string;
  technologiesKnown?: string;
  bio?: string;
  availableForMentoring?: boolean;
  recordVersion: number;
}
