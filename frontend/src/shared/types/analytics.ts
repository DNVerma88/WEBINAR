// ── Analytics types ──────────────────────────────────────────────────────────

export interface TopCategoryItem {
  id: string;
  name: string;
  sessionCount: number;
  assetCount: number;
}

export interface AnalyticsSummaryResponse {
  totalSessions: number;
  totalAssets: number;
  totalUsers: number;
  avgQuizPassRate: number;
  weeklyActiveUsers: number;
  topCategories: TopCategoryItem[];
}

export interface HeatmapCell {
  categoryId: string;
  categoryName: string;
  department: string;
  engagementScore: number;
  sessionCount: number;
  assetCount: number;
  quizPassRate: number;
}

export interface KnowledgeGapHeatmapResponse {
  cells: HeatmapCell[];
  departments: string[];
  categories: string[];
}

export interface CategorySkillCoverage {
  categoryId: string;
  categoryName: string;
  totalTagCount: number;
  coveredTagCount: number;
  coveragePercent: number;
  topCoveredSkills: string[];
  gapSkills: string[];
}

export interface SkillCoverageReportResponse {
  categories: CategorySkillCoverage[];
  overallCoveragePercent: number;
}

export interface ContentFreshnessItem {
  id: string;
  contentType: string;
  title: string;
  createdDate: string;
  ageDays: number;
}

export interface ContentFreshnessReportResponse {
  freshCount: number;
  recentCount: number;
  agingCount: number;
  staleCount: number;
  stalestItems: ContentFreshnessItem[];
}

export interface LearningFunnelResponse {
  discovered: number;
  registered: number;
  attended: number;
  rated: number;
  quizPassed: number;
  registrationRate: number;
  attendanceRate: number;
  ratingRate: number;
  passRate: number;
}

export interface CohortCompletionItem {
  learningPathId: string;
  title: string;
  totalEnrollments: number;
  completedCount: number;
  completionRate: number;
  avgCompletionDays: number;
}

export interface CohortCompletionRatesResponse {
  items: CohortCompletionItem[];
}

export interface DepartmentEngagementItem {
  department: string;
  sessionsAttended: number;
  assetsCreated: number;
  totalXpEarned: number;
  engagementScore: number;
}

export interface DepartmentEngagementScoreResponse {
  departments: DepartmentEngagementItem[];
}

export interface CategoryRetentionItem {
  categoryId: string;
  categoryName: string;
  quizAttempts: number;
  passRate: number;
  avgDaysBetweenAttempts: number;
  retentionScore: number;
}

export interface KnowledgeRetentionScoreResponse {
  categories: CategoryRetentionItem[];
  overallRetentionScore: number;
}
