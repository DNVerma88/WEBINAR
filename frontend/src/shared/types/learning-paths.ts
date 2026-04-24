import type { DifficultyLevel } from './sessions';

export const DifficultyLevelLabel: Record<string, string> = {
  Beginner: 'Beginner',
  Intermediate: 'Intermediate',
  Advanced: 'Advanced',
};

export enum LearningPathItemType {
  Session = 'Session',
  KnowledgeAsset = 'KnowledgeAsset',
}

export interface LearningPathDto {
  id: string;
  title: string;
  slug: string;
  description?: string;
  objective?: string;
  categoryId?: string;
  categoryName?: string;
  difficultyLevel: DifficultyLevel;
  estimatedDurationMinutes: number;
  isPublished: boolean;
  isAssignable: boolean;
  coverImageUrl?: string;
  itemCount: number;
}

export interface LearningPathItemDto {
  id: string;
  itemType: LearningPathItemType;
  sessionId?: string;
  sessionTitle?: string;
  knowledgeAssetId?: string;
  assetTitle?: string;
  orderSequence: number;
  isRequired: boolean;
}

export interface LearningPathDetailDto extends LearningPathDto {
  items: LearningPathItemDto[];
  enrolledCount: number;
}

export interface EnrolmentProgressDto {
  userId: string;
  learningPathId: string;
  progressPercentage: number;
  completedItemCount: number;
  totalItemCount: number;
  startedAt?: string;
  completedAt?: string;
}

export interface LearningPathCertificateDto {
  id: string;
  userId: string;
  learningPathId: string;
  certificateNumber: string;
  certificateUrl?: string;
  issuedAt: string;
}

export interface UserEnrolmentDto {
  learningPathId: string;
  pathTitle: string;
  pathSlug: string;
  progressPercentage: number;
  completedItemCount: number;
  totalItemCount: number;
  enrolmentType: number;
  startedAt?: string;
  completedAt?: string;
  deadlineAt?: string;
}

export interface GetLearningPathsRequest {
  searchTerm?: string;
  difficultyLevel?: DifficultyLevel;
  categoryId?: string;
  isPublished?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export interface AddLearningPathItemRequest {
  itemType: LearningPathItemType; // 0 = Session, 1 = KnowledgeAsset
  sessionId?: string;
  knowledgeAssetId?: string;
  isRequired?: boolean;
}

export interface CreateLearningPathRequest {
  title: string;
  description?: string;
  objective?: string;
  difficultyLevel?: DifficultyLevel;
  estimatedDurationMinutes?: number;
  categoryId?: string;
}

export interface UpdateLearningPathRequest {
  title: string;
  description?: string;
  objective?: string;
  isPublished: boolean;
  isAssignable: boolean;
  estimatedDurationMinutes?: number;
}

export enum CohortStatus {
  Scheduled = 'Scheduled',
  Active = 'Active',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
}

export const CohortStatusLabel: Record<string, string> = {
  Scheduled: 'Scheduled',
  Active: 'Active',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
};

export interface LearningPathCohortDto {
  id: string;
  learningPathId: string;
  name: string;
  description?: string;
  startDate: string;
  endDate?: string;
  maxParticipants?: number;
  status: CohortStatus;
  createdDate: string;
}

export interface CreateLearningPathCohortRequest {
  name: string;
  description?: string;
  startDate: string;
  endDate?: string;
  maxParticipants?: number;
}

export interface UpdateLearningPathCohortRequest {
  name: string;
  description?: string;
  startDate: string;
  endDate?: string;
  maxParticipants?: number;
  status: CohortStatus;
}
