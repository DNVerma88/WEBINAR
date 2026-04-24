// ── Peer Review types ──────────────────────────────────────────────────────────

export type AssetReviewStatus = 'Pending' | 'Approved' | 'Rejected' | 'RevisionRequested';

export interface AssetReviewDto {
  id: string;
  knowledgeAssetId: string;
  assetTitle: string;
  reviewerId: string;
  reviewerName: string;
  nominatedByUserId: string;
  nominatedByUserName: string;
  nominatedAt: string;
  status: AssetReviewStatus;
  comments?: string;
  reviewedAt?: string;
}

export interface NominateReviewerRequest {
  reviewerId: string;
}

export interface SubmitReviewRequest {
  status: Exclude<AssetReviewStatus, 'Pending'>;
  comments?: string;
}

export interface GetPendingReviewsRequest {
  pageNumber?: number;
  pageSize?: number;
}
