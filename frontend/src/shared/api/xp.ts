import axiosClient from './axiosClient';
import type { UserXpDto, UserStreakDto, SkillEndorsementDto } from '../types';

export const xpApi = {
  getUserXp: (userId: string, signal?: AbortSignal) =>
    axiosClient.get<UserXpDto>(`/users/${userId}/xp`, { signal }).then((r) => r.data),

  getUserStreak: (userId: string, signal?: AbortSignal) =>
    axiosClient.get<UserStreakDto>(`/users/${userId}/streak`, { signal }).then((r) => r.data),

  getUserEndorsements: (userId: string, signal?: AbortSignal) =>
    axiosClient.get<SkillEndorsementDto[]>(`/users/${userId}/endorsements`, { signal }).then((r) => r.data),
};
