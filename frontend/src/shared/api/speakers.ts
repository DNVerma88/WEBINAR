import axiosClient from './axiosClient';
import type { SpeakerDto, SpeakerDetailDto, GetSpeakersRequest, PagedList } from '../types';

export const speakersApi = {
  getSpeakers: (params?: GetSpeakersRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<SpeakerDto>>('/speakers', { params, signal }).then((r) => r.data),

  getSpeakerById: (id: string, signal?: AbortSignal) =>
    axiosClient.get<SpeakerDetailDto>(`/speakers/${id}`, { signal }).then((r) => r.data),
};
