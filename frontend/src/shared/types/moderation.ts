// ── Moderation types ──────────────────────────────────────────────────────────

export type FlaggedContentType = 'Session' | 'KnowledgeAsset' | 'Comment' | 'CommunityWikiPage' | 'SessionProposal';
export type FlagReason = 'Inappropriate' | 'Spam' | 'Inaccurate' | 'Duplicate' | 'Other';
export type FlagStatus = 'Pending' | 'Reviewed' | 'Dismissed' | 'ActionTaken';

export interface ContentFlagDto {
  id: string;
  flaggedByUserId: string;
  flaggedByUserName: string;
  contentType: FlaggedContentType;
  contentId: string;
  reason: FlagReason;
  notes?: string;
  status: FlagStatus;
  reviewedByUserId?: string;
  reviewedByUserName?: string;
  reviewedAt?: string;
  reviewNotes?: string;
  createdDate: string;
}

export interface UserSuspensionDto {
  id: string;
  userId: string;
  userName: string;
  suspendedByUserId: string;
  suspendedByUserName: string;
  reason: string;
  suspendedAt: string;
  expiresAt?: string;
  isActive: boolean;
  liftedByUserId?: string;
  liftedByUserName?: string;
  liftedAt?: string;
  liftReason?: string;
}

export interface FlagContentRequest {
  contentType: FlaggedContentType;
  contentId: string;
  reason: FlagReason;
  notes?: string;
}

export interface GetContentFlagsRequest {
  status?: FlagStatus;
  contentType?: FlaggedContentType;
  pageNumber?: number;
  pageSize?: number;
}

export interface ReviewFlagRequest {
  status: Exclude<FlagStatus, 'Pending'>;
  reviewNotes?: string;
}

export interface SuspendUserRequest {
  userId: string;
  reason: string;
  expiresAt?: string;
}

export interface LiftSuspensionRequest {
  liftReason: string;
}

export interface BulkSessionStatusRequest {
  sessionIds: string[];
  status: string;
}
