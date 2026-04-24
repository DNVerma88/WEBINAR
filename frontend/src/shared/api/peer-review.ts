import axiosClient from './axiosClient';
import type {
  AssetReviewDto,
  NominateReviewerRequest,
  SubmitReviewRequest,
  GetPendingReviewsRequest,
  PagedList,
} from '../types';

export const peerReviewApi = {
  nominateReviewer: (assetId: string, data: NominateReviewerRequest) =>
    axiosClient.post<AssetReviewDto>(`/knowledge-assets/${assetId}/reviews/nominate`, data).then((r) => r.data),

  getAssetReviews: (assetId: string, signal?: AbortSignal) =>
    axiosClient.get<AssetReviewDto[]>(`/knowledge-assets/${assetId}/reviews`, { signal }).then((r) => r.data),

  submitReview: (reviewId: string, data: SubmitReviewRequest) =>
    axiosClient.put<AssetReviewDto>(`/knowledge-assets/reviews/${reviewId}`, data).then((r) => r.data),

  getPendingReviews: (params?: GetPendingReviewsRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<AssetReviewDto>>('/knowledge-assets/reviews/pending', { params, signal }).then((r) => r.data),
};
