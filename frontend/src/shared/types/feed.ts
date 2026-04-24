import type { CommunityPostSummaryDto } from './community-posts';

export interface UserSummaryDto {
  id: string;
  fullName: string;
  avatarUrl?: string;
  isFollowedByCurrentUser: boolean;
}

export interface FeedPostDto extends CommunityPostSummaryDto {
  communityName: string;
  communitySlug: string;
}

export interface TrendingPostDto extends FeedPostDto {
  trendingScore: number;
}

export interface FeedRequest {
  afterId?: string;
  afterDate?: string;
  pageSize?: number;
}

export interface PostSeriesDto {
  id: string;
  communityId: string;
  authorId: string;
  authorName: string;
  title: string;
  slug: string;
  description?: string;
  postCount: number;
  posts: CommunityPostSummaryDto[];
}

export interface CreateSeriesRequest {
  title: string;
  description?: string;
}

export interface UpdateSeriesRequest {
  title: string;
  description?: string;
}

export enum ReportReason {
  Spam = 0,
  Abuse = 1,
  Misinformation = 2,
  NSFW = 3,
  Copyright = 4,
}

export enum ReportStatus {
  Open = 0,
  Resolved = 1,
  Dismissed = 2,
}

export interface ContentReportDto {
  id: string;
  reporterName: string;
  targetPostId?: string;
  targetPostTitle?: string;
  targetCommentId?: string;
  reasonCode: ReportReason;
  description?: string;
  status: ReportStatus;
  resolverName?: string;
  resolvedAt?: string;
  createdDate: string;
}

export interface ReportContentRequest {
  targetPostId?: string;
  targetCommentId?: string;
  reasonCode: ReportReason;
  description?: string;
}
