export type LeaderboardType = 'ByXp' | 'ByContributions' | 'ByAttendance' | 'ByRating' | 'ByMentoring' | 'ByDepartment';

export interface XpEventDto {
  eventType: number;
  xpAmount: number;
  earnedAt: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
}

export interface UserXpDto {
  userId: string;
  totalXp: number;
  recentEvents: XpEventDto[];
}

export interface LeaderboardEntryDto {
  rank: number;
  userId: string;
  displayName: string;
  score: number;
  avatarUrl?: string;
}

export interface LeaderboardDto {
  type: string;
  month?: number;
  year?: number;
  entries: LeaderboardEntryDto[];
}
