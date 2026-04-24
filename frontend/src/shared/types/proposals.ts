import type { SessionFormat, DifficultyLevel } from './sessions';

export enum ProposalStatus {
  Draft = 'Draft',
  Submitted = 'Submitted',
  ManagerReview = 'ManagerReview',
  KnowledgeTeamReview = 'KnowledgeTeamReview',
  Published = 'Published',
  Scheduled = 'Scheduled',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  Rejected = 'Rejected',
  RevisionRequested = 'RevisionRequested',
}

export const PROPOSAL_STATUS_LABELS: Record<ProposalStatus, string> = {
  [ProposalStatus.Draft]: 'Draft',
  [ProposalStatus.Submitted]: 'Submitted',
  [ProposalStatus.ManagerReview]: 'Manager Review',
  [ProposalStatus.KnowledgeTeamReview]: 'Knowledge Team Review',
  [ProposalStatus.Published]: 'Published',
  [ProposalStatus.Scheduled]: 'Scheduled',
  [ProposalStatus.InProgress]: 'In Progress',
  [ProposalStatus.Completed]: 'Completed',
  [ProposalStatus.Cancelled]: 'Cancelled',
  [ProposalStatus.Rejected]: 'Rejected',
  [ProposalStatus.RevisionRequested]: 'Revision Requested',
};

export interface SessionProposalDto {
  id: string;
  title: string;
  description?: string;
  categoryId: string;
  categoryName: string;
  topic: string;
  proposerId: string;
  proposerName: string;
  status: ProposalStatus;
  format: SessionFormat;
  difficultyLevel: DifficultyLevel;
  estimatedDurationMinutes: number;
  targetAudience?: string;
  prerequisites?: string;
  expectedOutcomes?: string;
  createdDate: string;
  recordVersion: number;
}

export interface CreateSessionProposalRequest {
  title: string;
  description?: string;
  categoryId: string;
  topic: string;
  format: SessionFormat;
  difficultyLevel: DifficultyLevel;
  estimatedDurationMinutes: number;
  targetAudience?: string;
  prerequisites?: string;
  expectedOutcomes?: string;
}

export interface UpdateSessionProposalRequest {
  title: string;
  description?: string;
  categoryId: string;
  topic: string;
  format: SessionFormat;
  difficultyLevel: DifficultyLevel;
  estimatedDurationMinutes: number;
  targetAudience?: string;
  prerequisites?: string;
  expectedOutcomes?: string;
  recordVersion: number;
}

export interface ApproveProposalRequest {
  comment?: string;
}

export interface RejectProposalRequest {
  comment: string;
}

export interface RequestRevisionRequest {
  comment: string;
}

export interface GetProposalsRequest {
  search?: string;
  status?: ProposalStatus;
  categoryId?: string;
  page?: number;
  pageSize?: number;
}
