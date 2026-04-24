import axiosClient from './axiosClient';
import type { LeaderboardDto, LeaderboardType } from '../types';

export const leaderboardsApi = {
  getLeaderboard: (type: LeaderboardType = 'ByXp', month?: number, year?: number, signal?: AbortSignal) =>
    axiosClient
      .get<LeaderboardDto>('/leaderboards', { params: { type, month, year }, signal })
      .then((r) => r.data),
};
