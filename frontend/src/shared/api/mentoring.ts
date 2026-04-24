import axiosClient from './axiosClient';
import type { MentorMenteeDto, RequestMentorRequest, PagedList } from '../types';

export const mentoringApi = {
  getPairings: (signal?: AbortSignal) =>
    axiosClient.get<PagedList<MentorMenteeDto>>('/mentoring', { signal }).then((r) => r.data),

  requestMentor: (data: RequestMentorRequest) =>
    axiosClient.post<MentorMenteeDto>('/mentoring/requests', data).then((r) => r.data),

  accept: (id: string) =>
    axiosClient.post<MentorMenteeDto>(`/mentoring/${id}/accept`).then((r) => r.data),

  decline: (id: string) =>
    axiosClient.post<MentorMenteeDto>(`/mentoring/${id}/decline`).then((r) => r.data),
};
