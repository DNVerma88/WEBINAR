export interface KnowledgeBundleItemDto {
  id: string;
  knowledgeAssetId: string;
  assetTitle: string;
  assetUrl: string;
  assetType: number;
  orderSequence: number;
  notes?: string;
}

export interface KnowledgeBundleDto {
  id: string;
  title: string;
  description?: string;
  createdByUserId: string;
  createdByName: string;
  categoryId?: string;
  categoryName?: string;
  isPublished: boolean;
  coverImageUrl?: string;
  itemCount: number;
  createdDate: string;
}

export interface KnowledgeBundleDetailDto extends KnowledgeBundleDto {
  items: KnowledgeBundleItemDto[];
}

export interface GetBundlesRequest {
  searchTerm?: string;
  categoryId?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface CreateKnowledgeBundleRequest {
  title: string;
  description?: string;
  categoryId?: string;
  coverImageUrl?: string;
}

export interface UpdateKnowledgeBundleRequest {
  title: string;
  description?: string;
  categoryId?: string;
  coverImageUrl?: string;
  isPublished: boolean;
}

export interface AddBundleItemRequest {
  knowledgeAssetId: string;
  orderSequence: number;
  notes?: string;
}
