export interface UserStreakDto {
  userId: string;
  currentStreakDays: number;
  longestStreakDays: number;
  lastActivityDate: string;
  streakFrozenUntil?: string;
}
