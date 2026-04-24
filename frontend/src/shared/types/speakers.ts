export interface SpeakerDto {
  userId: string;
  fullName: string;
  profilePhotoUrl?: string;
  department?: string;
  designation?: string;
  areasOfExpertise?: string;
  technologiesKnown?: string;
  bio?: string;
  averageRating: number;
  totalSessionsDelivered: number;
  followerCount: number;
  isKnowledgeBroker: boolean;
  isFollowedByCurrentUser: boolean;
  availableForMentoring?: boolean;
}

export interface SpeakerSessionDto {
  id: string;
  title: string;
  scheduledAt: string;
  categoryName?: string;
  durationMinutes: number;
  averageRating: number;
}

export interface SpeakerDetailDto extends SpeakerDto {
  recentSessions: SpeakerSessionDto[];
}

export interface GetSpeakersRequest {
  searchTerm?: string;
  expertiseArea?: string;
  technology?: string;
  categoryId?: string;
  pageNumber?: number;
  pageSize?: number;
}
