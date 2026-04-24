export interface WikiPageDto {
  id: string;
  communityId: string;
  authorId: string;
  authorName: string;
  title: string;
  slug: string;
  contentMarkdown: string;
  parentPageId?: string;
  orderSequence: number;
  isPublished: boolean;
  viewCount: number;
  createdDate: string;
}

export interface CreateWikiPageRequest {
  title: string;
  contentMarkdown: string;
  parentPageId?: string;
  isPublished?: boolean;
}

export interface UpdateWikiPageRequest {
  title: string;
  contentMarkdown: string;
  isPublished: boolean;
}
