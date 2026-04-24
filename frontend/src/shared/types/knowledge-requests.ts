export enum KnowledgeRequestStatus {
  Open = 'Open',
  InProgress = 'InProgress',
  Addressed = 'Addressed',
  Closed = 'Closed',
}

export const KNOWLEDGE_REQUEST_STATUS_LABELS: Record<KnowledgeRequestStatus, string> = {
  [KnowledgeRequestStatus.Open]: 'Open',
  [KnowledgeRequestStatus.InProgress]: 'In Progress',
  [KnowledgeRequestStatus.Addressed]: 'Addressed',
  [KnowledgeRequestStatus.Closed]: 'Closed',
};

export interface KnowledgeRequestDto {
  id: string;
  title: string;
  description?: string;
  requesterId: string;
  requesterName: string;
  status: KnowledgeRequestStatus;
  upvoteCount: number;
  hasUpvoted: boolean;
  bountyXp: number;
  categoryId?: string;
  categoryName?: string;
  createdDate: string;
  claimedByUserId?: string;
  claimedByName?: string;
  addressedBySessionId?: string;
}

export interface CreateKnowledgeRequestRequest {
  title: string;
  description?: string;
  bountyXp?: number;
}

export interface CloseKnowledgeRequestRequest {
  reason?: string;
}

export interface AddressKnowledgeRequestRequest {
  sessionId: string;
}

export interface GetKnowledgeRequestsRequest {
  search?: string;
  status?: KnowledgeRequestStatus;
  page?: number;
  pageSize?: number;
}
