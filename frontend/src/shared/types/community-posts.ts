export enum PostType {
  Article = 0,
  Discussion = 1,
  Question = 2,
  TIL = 3,
  Showcase = 4,
}

export enum PostStatus {
  Draft = 0,
  Published = 1,
  Pinned = 2,
  Archived = 3,
  Scheduled = 4,
}

export enum ReactionType {
  Like = 0,
  Unicorn = 1,
  MindBlown = 2,
  Fire = 3,
  Clap = 4,
}

export enum PostSortBy {
  Latest = 0,
  Trending = 1,
  Top = 2,
}

export interface CommunityPostTagDto {
  tagId: string;
  name: string;
  slug: string;
}

export interface CommunityPostSummaryDto {
  id: string;
  communityId: string;
  authorId: string;
  authorName: string;
  authorAvatarUrl?: string;
  title: string;
  slug: string;
  coverImageUrl?: string;
  postType: PostType;
  status: PostStatus;
  readingTimeMinutes: number;
  reactionCount: number;
  commentCount: number;
  viewCount: number;
  bookmarkCount: number;
  publishedAt?: string;
  isFeatured: boolean;
  hasBookmarked: boolean;
  tags: CommunityPostTagDto[];
}

export interface CommunityPostDetailDto extends CommunityPostSummaryDto {
  contentHtml: string;
  contentMarkdown: string;
  canonicalUrl?: string;
  seriesId?: string;
  seriesTitle?: string;
  userReactions: ReactionType[];
}

export interface PostReactionResultDto {
  postId: string;
  reactionCounts: Record<ReactionType, number>;
  userReactions: ReactionType[];
}

export interface PostCommentDto {
  id: string;
  postId: string;
  authorId: string;
  authorName: string;
  authorAvatarUrl?: string;
  parentCommentId?: string;
  bodyMarkdown: string;
  isDeleted: boolean;
  likeCount: number;
  createdDate: string;
  replies: PostCommentDto[];
}

export interface PostBookmarkToggleResult {
  postId: string;
  isBookmarked: boolean;
  bookmarkCount: number;
}

export interface GetPostsRequest {
  tagSlug?: string;
  postType?: PostType;
  sortBy?: PostSortBy;
  pageNumber?: number;
  pageSize?: number;
}

export interface CreatePostRequest {
  title: string;
  contentMarkdown: string;
  postType: PostType;
  coverImageUrl?: string;
  canonicalUrl?: string;
  tagSlugs: string[];
  publishImmediately: boolean;
  scheduledAt?: string;
}

export interface UpdatePostRequest {
  title: string;
  contentMarkdown: string;
  coverImageUrl?: string;
  canonicalUrl?: string;
  tagSlugs: string[];
  status?: PostStatus;
}

export interface AddCommentRequest {
  bodyMarkdown: string;
  parentCommentId?: string;
}

export interface ToggleReactionRequest {
  reactionType: ReactionType;
}

export const POST_TYPE_LABELS: Record<PostType, string> = {
  [PostType.Article]: 'Article',
  [PostType.Discussion]: 'Discussion',
  [PostType.Question]: 'Question',
  [PostType.TIL]: 'TIL',
  [PostType.Showcase]: 'Showcase',
};

export const REACTION_EMOJIS: Record<ReactionType, string> = {
  [ReactionType.Like]: '❤️',
  [ReactionType.Unicorn]: '🦄',
  [ReactionType.MindBlown]: '🤯',
  [ReactionType.Fire]: '🔥',
  [ReactionType.Clap]: '👏',
};
