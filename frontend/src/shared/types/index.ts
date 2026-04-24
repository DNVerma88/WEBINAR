// Barrel — re-exports every domain type so all existing imports continue to work.
// Import from domain files directly when you need a subset:
//   import type { SessionDto } from '@/shared/types/sessions';

export * from './common';
export * from './auth';
export * from './users';
export * from './categories';
export * from './tags';
export * from './sessions';
export * from './proposals';
export * from './communities';
export * from './knowledge-requests';
export * from './notifications';
export * from './speakers';
export * from './learning-paths';
export * from './knowledge-assets';
export * from './knowledge-bundles';
export * from './gamification';
export * from './streaks';
export * from './mentoring';
export * from './endorsements';
export * from './wiki';
export * from './analytics';
export * from './moderation';
export * from './peer-review';
export * from './speaker-marketplace';
export * from './ai';
export * from './community-posts';
