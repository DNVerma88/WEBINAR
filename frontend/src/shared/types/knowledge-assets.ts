export enum KnowledgeAssetType {
  Recording = 'Recording',
  Slides = 'Slides',
  Code = 'Code',
  Documentation = 'Documentation',
  FAQ = 'FAQ',
  AfterActionReview = 'AfterActionReview',
  Certificate = 'Certificate',
  Bundle = 'Bundle',
}

export const KnowledgeAssetTypeLabel: Record<string, string> = {
  Recording: 'Recording',
  Slides: 'Slides',
  Code: 'Code',
  Documentation: 'Documentation',
  FAQ: 'FAQ',
  AfterActionReview: 'After Action Review',
  Certificate: 'Certificate',
  Bundle: 'Bundle',
};

export interface KnowledgeAssetDto {
  id: string;
  sessionId?: string;
  title: string;
  url: string;
  description?: string;
  assetType: KnowledgeAssetType;
  viewCount: number;
  downloadCount: number;
  isPublic: boolean;
  isVerified: boolean;
  createdDate: string;
  createdBy: string;
}

export interface CreateKnowledgeAssetRequest {
  sessionId?: string;
  title: string;
  url: string;
  description?: string;
  assetType: KnowledgeAssetType;
  isPublic: boolean;
}

export interface GetAssetsRequest {
  sessionId?: string;
  assetType?: KnowledgeAssetType;
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
}
