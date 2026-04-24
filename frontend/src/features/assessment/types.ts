// ── Enums ─────────────────────────────────────────────────────────────────────

export enum AssessmentPeriodFrequency {
  Weekly     = 'Weekly',
  BiWeekly   = 'BiWeekly',
  Monthly    = 'Monthly',
  Quarterly  = 'Quarterly',
  HalfYearly = 'HalfYearly',
  Annual     = 'Annual',
}

export enum AssessmentPeriodStatus {
  Draft     = 'Draft',
  Open      = 'Open',
  Closed    = 'Closed',
  Published = 'Published',
}

export enum AssessmentStatus {
  Draft     = 'Draft',
  Submitted = 'Submitted',
  Reopened  = 'Reopened',
}

export enum AssessmentActionType {
  Created          = 0,
  Updated          = 1,
  Submitted        = 2,
  Reopened         = 3,
  PrimaryLeadChanged = 4,
  CoLeadAssigned    = 5,
  CoLeadRemoved     = 6,
  MemberAssigned    = 7,
  MemberRemoved     = 8,
  PeriodOpened     = 9,
  PeriodClosed     = 10,
  PeriodPublished  = 11,
}

// ── Group ─────────────────────────────────────────────────────────────────────

export interface AssessmentGroupDto {
  id: string;
  groupName: string;
  groupCode: string;
  description?: string;
  primaryLeadUserId: string;
  primaryLeadName: string;
  assessmentCategory?: string;
  coLeadUserId?: string;
  coLeadName?: string;
  activeEmployeeCount: number;
  isActive: boolean;
  createdDate: string;
  recordVersion: number;
}

export interface AssessmentGroupFilter {
  searchTerm?: string;
  isActive?: boolean;
  primaryLeadId?: string;
  coLeadId?: string;
  pageNumber: number;
  pageSize: number;
}

export interface CreateAssessmentGroupRequest {
  groupName: string;
  groupCode: string;
  description?: string;
  primaryLeadUserId: string;
  coLeadUserId?: string;
  assessmentCategory?: string;
}

export interface UpdateAssessmentGroupRequest {
  groupName: string;
  description?: string;
  primaryLeadUserId: string;
  coLeadUserId?: string;
  assessmentCategory?: string;
  isActive: boolean;
  recordVersion: number;
}

export interface AssignGroupMemberRequest {
  userId: string;
  workRoleId?: string;
}

export interface GroupMemberDto {
  id: string;
  userId: string;
  fullName: string;
  designation?: string;
  department?: string;
  email?: string;
  workRoleId?: string;
  workRoleCode?: string;
  workRoleName?: string;
  effectiveFrom: string;
  effectiveTo?: string;
  isActive: boolean;
}

// ── Period ────────────────────────────────────────────────────────────────────

export interface AssessmentPeriodDto {
  id: string;
  name: string;
  frequency: AssessmentPeriodFrequency;
  startDate: string;
  endDate: string;
  year: number;
  weekNumber?: number;
  status: AssessmentPeriodStatus;
  isActive: boolean;
  recordVersion: number;
}

export interface AssessmentPeriodFilter {
  year?: number;
  status?: AssessmentPeriodStatus;
  frequency?: AssessmentPeriodFrequency;
  pageNumber: number;
  pageSize: number;
}

export interface CreateAssessmentPeriodRequest {
  name: string;
  frequency: AssessmentPeriodFrequency;
  startDate: string;
  endDate: string;
  year: number;
  weekNumber?: number;
}

export interface UpdateAssessmentPeriodRequest {
  name: string;
  startDate: string;
  endDate: string;
  weekNumber?: number;
  recordVersion: number;
}

export interface GeneratePeriodsRequest {
  year: number;
  frequency: AssessmentPeriodFrequency;
}

// ── Rating Scale ──────────────────────────────────────────────────────────────

export interface RatingScaleDto {
  id: string;
  code: string;
  name: string;
  numericValue: number;
  displayOrder: number;
  isActive: boolean;
  recordVersion: number;
}

export interface CreateRatingScaleRequest {
  code: string;
  name: string;
  numericValue: number;
  displayOrder: number;
}

export interface UpdateRatingScaleRequest {
  code: string;
  name: string;
  numericValue: number;
  displayOrder: number;
  isActive: boolean;
  recordVersion: number;
}

// ── Rubric ────────────────────────────────────────────────────────────────────

export interface RubricDefinitionDto {
  id: string;
  designationCode: string;
  ratingScaleId: string;
  ratingScaleName: string;
  ratingScaleValue: number;
  behaviorDescription: string;
  processDescription: string;
  evidenceDescription: string;
  versionNo: number;
  effectiveFrom: string;
  effectiveTo?: string;
  isActive: boolean;
}

export interface CreateRubricRequest {
  designationCode: string;
  ratingScaleId: string;
  behaviorDescription: string;
  processDescription: string;
  evidenceDescription: string;
  effectiveFrom: string;
  effectiveTo?: string;
}

export interface UpdateRubricRequest {
  behaviorDescription: string;
  processDescription: string;
  evidenceDescription: string;
  effectiveFrom: string;
  effectiveTo?: string;
}

// ── Parameter ─────────────────────────────────────────────────────────────────

export interface ParameterMasterDto {
  id: string;
  name: string;
  code: string;
  description?: string;
  category: string;
  displayOrder: number;
  isActive: boolean;
  recordVersion: number;
}

export interface CreateParameterRequest {
  name: string;
  code: string;
  description?: string;
  category: string;
  displayOrder: number;
}

export interface UpdateParameterRequest {
  name: string;
  description?: string;
  category: string;
  displayOrder: number;
  isActive: boolean;
  recordVersion: number;
}

export interface RoleParameterMappingDto {
  id: string;
  designationCode: string;
  parameterId: string;
  parameterName: string;
  weightage: number;
  displayOrder: number;
  isMandatory: boolean;
  isActive: boolean;
  recordVersion: number;
}

export interface UpsertRoleMappingRequest {
  designationCode: string;
  parameterId: string;
  weightage: number;
  displayOrder: number;
  isMandatory: boolean;
}

// ── Assessment ────────────────────────────────────────────────────────────────

export interface EmployeeAssessmentDto {
  id: string;
  userId: string;
  employeeName: string;
  department?: string;
  designation?: string;
  groupId: string;
  groupName: string;
  assessmentPeriodId: string;
  periodName: string;
  ratingScaleId: string;
  ratingScaleName: string;
  ratingValue: number;
  comment?: string;
  evidenceNotes?: string;
  status: AssessmentStatus;
  previousRatingValue?: number;
  previousRatingName?: string;
  ratingChange?: number;
  submittedOn?: string;
  recordVersion: number;
}

export interface AssessmentFilter {
  groupId?: string;
  periodId?: string;
  status?: number;
  search?: string;
  pageNumber: number;
  pageSize: number;
}

export interface AssessmentGridRequest {
  groupId: string;
  periodId: string;
  search?: string;
  rating?: number;
  status?: number;
}

export interface SaveAssessmentDraftRequest {
  userId: string;
  groupId: string;
  assessmentPeriodId: string;
  ratingScaleId: string;
  ratingValue: number;
  comment?: string;
  evidenceNotes?: string;
}

export interface BulkSaveAssessmentRequest {
  assessmentPeriodId: string;
  groupId: string;
  assessments: SaveAssessmentDraftRequest[];
}

export interface BulkSubmitRequest {
  groupId: string;
  periodId: string;
}

export interface ReopenAssessmentRequest {
  remarks: string;
}

// ── Dashboard ─────────────────────────────────────────────────────────────────

export interface RatingBandCount {
  ratingName: string;
  numericValue: number;
  count: number;
}

export interface PrimaryLeadDashboardDto {
  totalEmployees: number;
  rated: number;
  pending: number;
  completionPercent: number;
  currentRatingDistribution: RatingBandCount[];
  previousRatingDistribution: RatingBandCount[];
  periodName: string;
  groupName: string;
}

export interface GroupCompletionRow {
  groupId: string;
  groupName: string;
  primaryLeadName: string;
  totalEmployees: number;
  submitted: number;
  pending: number;
  completionPercent: number;
}

export interface AdminDashboardDto {
  totalGroups: number;
  totalEmployees: number;
  avgCompletionPercent: number;
  periodName: string;
  groupCompletions: GroupCompletionRow[];
  ratingDistribution: RatingBandCount[];
}

export interface CoLeadDashboardDto {
  groups: GroupCompletionRow[];
  ratingDistribution: RatingBandCount[];
  periodName: string;
}

export interface PeriodTrendRow {
  periodName: string;
  avgScore: number;
  totalRated: number;
}

export interface GroupTrendRow {
  groupName: string;
  currentScore: number;
  change?: number;
}

export interface ExecutiveDashboardDto {
  orgMaturityScore: number;
  totalAssessed: number;
  periodName: string;
  ratingDistribution: RatingBandCount[];
  topImprovingGroups: GroupTrendRow[];
  laggingGroups: GroupTrendRow[];
  historicalTrend: PeriodTrendRow[];
}

// ── Reports ───────────────────────────────────────────────────────────────────

export interface DetailedReportFilter {
  periodId?: string;
  groupId?: string;
  primaryLeadId?: string;
  coLeadId?: string;
  designation?: string;
  department?: string;
  rating?: number;
  status?: number;
  search?: string;
  pageNumber: number;
  pageSize: number;
}

export interface DetailedAssessmentReportRow {
  employeeName: string;
  designation?: string;
  department?: string;
  groupName: string;
  primaryLeadName: string;
  coLeadName?: string;
  periodName: string;
  previousRatingValue?: number;
  previousRatingName?: string;
  currentRatingValue: number;
  currentRatingName: string;
  ratingChange?: number;
  comment?: string;
  evidenceNotes?: string;
  status: string;
}

export interface CompletionReportDto {
  totalExpected: number;
  completed: number;
  pending: number;
  completionPct: number;
  byGroup: GroupCompletionRow[];
}

export interface GroupDistributionRow {
  groupName: string;
  distribution: RatingBandCount[];
}

export interface RoleDistributionRow {
  designation: string;
  avgScore: number;
  distribution: RatingBandCount[];
}

export interface EmployeeHistoryRow {
  periodName: string;
  ratingValue: number;
  ratingName: string;
  change?: number;
  comment?: string;
  status: string;
  startDate: string;
}

export interface TrendReportFilter {
  fromYear: number;
  toYear: number;
  groupId?: string;
}

export interface TrendReportDto {
  periods: PeriodTrendRow[];
  improvingCount: number;
  decliningCount: number;
  stableCount: number;
}

export interface GroupRiskRow {
  groupName: string;
  avgScore: number;
  missing: number;
  riskLevel: string;
}

export interface ImprovementReportFilter {
  fromPeriodId: string;
  toPeriodId: string;
}

export interface ImprovementRow {
  employeeName: string;
  groupName: string;
  fromRating: number;
  toRating: number;
  change: number;
}

export interface ExportFilter {
  periodId?: string;
  groupId?: string;
  designation?: string;
  department?: string;
  rating?: number;
  reportType?: string;
}

// ── Audit ─────────────────────────────────────────────────────────────────────

export interface AssessmentAuditLogDto {
  id: string;
  employeeAssessmentId?: string;
  relatedEntityType: string;
  relatedEntityId: string;
  actionType: string;
  oldValueJson?: string;
  newValueJson?: string;
  changedBy: string;
  changedByName: string;
  changedOn: string;
  remarks?: string;
}

export interface AuditFilter {
  entityId?: string;
  actionType?: number;
  changedBy?: string;
  changedByName?: string;
  from?: string;
  to?: string;
  pageNumber: number;
  pageSize: number;
}

// ── WorkRole ──────────────────────────────────────────────────────────────────

export interface WorkRoleDto {
  id: string;
  code: string;
  name: string;
  category: string;
  displayOrder: number;
  isActive: boolean;
  recordVersion: number;
}

export interface CreateWorkRoleRequest {
  code: string;
  name: string;
  category: string;
  displayOrder: number;
}

export interface UpdateWorkRoleRequest {
  name: string;
  category: string;
  displayOrder: number;
  isActive: boolean;
  recordVersion: number;
}

export interface WorkRoleRatingRow {
  workRoleCode: string;
  workRoleName: string;
  category: string;
  totalRated: number;
  avgScore: number;
  distribution: RatingBandCount[];
}
