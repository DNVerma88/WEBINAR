import axiosClient from '../../../shared/api/axiosClient';
import type {
  AssessmentGroupDto,
  AssessmentGroupFilter,
  CreateAssessmentGroupRequest,
  UpdateAssessmentGroupRequest,
  AssignGroupMemberRequest,
  GroupMemberDto,
  AssessmentPeriodDto,
  AssessmentPeriodFilter,
  CreateAssessmentPeriodRequest,
  UpdateAssessmentPeriodRequest,
  GeneratePeriodsRequest,
  RatingScaleDto,
  CreateRatingScaleRequest,
  UpdateRatingScaleRequest,
  RubricDefinitionDto,
  CreateRubricRequest,
  UpdateRubricRequest,
  ParameterMasterDto,
  CreateParameterRequest,
  UpdateParameterRequest,
  RoleParameterMappingDto,
  UpsertRoleMappingRequest,
  EmployeeAssessmentDto,
  AssessmentFilter,
  AssessmentGridRequest,
  SaveAssessmentDraftRequest,
  BulkSaveAssessmentRequest,
  BulkSubmitRequest,
  ReopenAssessmentRequest,
  PrimaryLeadDashboardDto,
  AdminDashboardDto,
  CoLeadDashboardDto,
  ExecutiveDashboardDto,
  DetailedReportFilter,
  DetailedAssessmentReportRow,
  CompletionReportDto,
  GroupDistributionRow,
  RoleDistributionRow,
  EmployeeHistoryRow,
  TrendReportFilter,
  TrendReportDto,
  GroupRiskRow,
  ImprovementReportFilter,
  ImprovementRow,
  ExportFilter,
  AssessmentAuditLogDto,
  AuditFilter,
  WorkRoleDto,
  CreateWorkRoleRequest,
  UpdateWorkRoleRequest,
  WorkRoleRatingRow,
} from '../types';
import type { PagedResult } from '../../../shared/types';

const BASE = '/assessment';

// ── Groups ────────────────────────────────────────────────────────────────────

export const assessmentGroupApi = {
  getGroups: (filter: Partial<AssessmentGroupFilter>) =>
    axiosClient.get<PagedResult<AssessmentGroupDto>>(`${BASE}/groups`, { params: filter }).then(r => r.data),

  getGroupById: (id: string) =>
    axiosClient.get<AssessmentGroupDto>(`${BASE}/groups/${id}`).then(r => r.data),

  createGroup: (data: CreateAssessmentGroupRequest) =>
    axiosClient.post<AssessmentGroupDto>(`${BASE}/groups`, data).then(r => r.data),

  updateGroup: (id: string, data: UpdateAssessmentGroupRequest) =>
    axiosClient.put<AssessmentGroupDto>(`${BASE}/groups/${id}`, data).then(r => r.data),

  deactivateGroup: (id: string) =>
    axiosClient.delete(`${BASE}/groups/${id}`).then(r => r.data),

  getMembers: (groupId: string) =>
    axiosClient.get<GroupMemberDto[]>(`${BASE}/groups/${groupId}/members`).then(r => r.data),

  addMember: (groupId: string, data: AssignGroupMemberRequest) =>
    axiosClient.post(`${BASE}/groups/${groupId}/members`, data).then(r => r.data),

  removeMember: (groupId: string, userId: string) =>
    axiosClient.delete(`${BASE}/groups/${groupId}/members/${userId}`).then(r => r.data),

  getCoLeads: (groupId: string) =>
    axiosClient.get<GroupMemberDto[]>(`${BASE}/groups/${groupId}/co-leads`).then(r => r.data),

  assignCoLead: (groupId: string, data: AssignGroupMemberRequest) =>
    axiosClient.post(`${BASE}/groups/${groupId}/co-leads`, data).then(r => r.data),

  removeCoLead: (groupId: string, userId: string) =>
    axiosClient.delete(`${BASE}/groups/${groupId}/co-leads/${userId}`).then(r => r.data),
};

// ── Periods ───────────────────────────────────────────────────────────────────

export const assessmentPeriodApi = {
  getPeriods: (filter: Partial<AssessmentPeriodFilter>) =>
    axiosClient.get<PagedResult<AssessmentPeriodDto>>(`${BASE}/periods`, { params: filter }).then(r => r.data),

  getPeriodById: (id: string) =>
    axiosClient.get<AssessmentPeriodDto>(`${BASE}/periods/${id}`).then(r => r.data),

  createPeriod: (data: CreateAssessmentPeriodRequest) =>
    axiosClient.post<AssessmentPeriodDto>(`${BASE}/periods`, data).then(r => r.data),

  updatePeriod: (id: string, data: UpdateAssessmentPeriodRequest) =>
    axiosClient.put<AssessmentPeriodDto>(`${BASE}/periods/${id}`, data).then(r => r.data),

  openPeriod: (id: string) =>
    axiosClient.post(`${BASE}/periods/${id}/open`).then(r => r.data),

  closePeriod: (id: string) =>
    axiosClient.post(`${BASE}/periods/${id}/close`).then(r => r.data),

  publishPeriod: (id: string) =>
    axiosClient.post(`${BASE}/periods/${id}/publish`).then(r => r.data),

  generatePeriods: (data: GeneratePeriodsRequest) =>
    axiosClient.post<AssessmentPeriodDto[]>(`${BASE}/periods/generate`, data).then(r => r.data),
};

// ── Rating Scales ─────────────────────────────────────────────────────────────

export const ratingScaleApi = {
  getScales: () =>
    axiosClient.get<RatingScaleDto[]>(`${BASE}/rating-scales`).then(r => r.data),

  createScale: (data: CreateRatingScaleRequest) =>
    axiosClient.post<RatingScaleDto>(`${BASE}/rating-scales`, data).then(r => r.data),

  updateScale: (id: string, data: UpdateRatingScaleRequest) =>
    axiosClient.put<RatingScaleDto>(`${BASE}/rating-scales/${id}`, data).then(r => r.data),

  deactivateScale: (id: string) =>
    axiosClient.delete(`${BASE}/rating-scales/${id}`).then(r => r.data),
};

// ── Rubrics ───────────────────────────────────────────────────────────────────

export const rubricApi = {
  getRubrics: (designationCode?: string) =>
    axiosClient.get<RubricDefinitionDto[]>(`${BASE}/rubrics`, { params: { designationCode } }).then(r => r.data),

  getCurrentRubrics: (designationCode: string) =>
    axiosClient.get<RubricDefinitionDto[]>(`${BASE}/rubrics/current`, { params: { designationCode } }).then(r => r.data),

  createRubric: (data: CreateRubricRequest) =>
    axiosClient.post<RubricDefinitionDto>(`${BASE}/rubrics`, data).then(r => r.data),

  updateRubric: (id: string, data: UpdateRubricRequest) =>
    axiosClient.put<RubricDefinitionDto>(`${BASE}/rubrics/${id}`, data).then(r => r.data),
};

// ── Parameters ────────────────────────────────────────────────────────────────

export const parameterApi = {
  getParameters: () =>
    axiosClient.get<ParameterMasterDto[]>(`${BASE}/parameters`).then(r => r.data),

  createParameter: (data: CreateParameterRequest) =>
    axiosClient.post<ParameterMasterDto>(`${BASE}/parameters`, data).then(r => r.data),

  updateParameter: (id: string, data: UpdateParameterRequest) =>
    axiosClient.put<ParameterMasterDto>(`${BASE}/parameters/${id}`, data).then(r => r.data),

  getRoleMappings: (designationCode?: string) =>
    axiosClient.get<RoleParameterMappingDto[]>(`${BASE}/parameters/role-mappings`, { params: { designationCode } }).then(r => r.data),

  upsertRoleMapping: (data: UpsertRoleMappingRequest) =>
    axiosClient.post<RoleParameterMappingDto>(`${BASE}/parameters/role-mappings`, data).then(r => r.data),

  removeRoleMapping: (id: string) =>
    axiosClient.delete(`${BASE}/parameters/role-mappings/${id}`).then(r => r.data),
};

// ── Assessments ───────────────────────────────────────────────────────────────

export const employeeAssessmentApi = {
  getAssessments: (filter: Partial<AssessmentFilter>) =>
    axiosClient.get<PagedResult<EmployeeAssessmentDto>>(`${BASE}/assessments`, { params: filter }).then(r => r.data),

  getAssessmentById: (id: string) =>
    axiosClient.get<EmployeeAssessmentDto>(`${BASE}/assessments/${id}`).then(r => r.data),

  getOrCreateDrafts: (data: AssessmentGridRequest) =>
    axiosClient.post<EmployeeAssessmentDto[]>(`${BASE}/assessments/grid`, data).then(r => r.data),

  saveDraft: (data: SaveAssessmentDraftRequest) =>
    axiosClient.post<EmployeeAssessmentDto>(`${BASE}/assessments/draft`, data).then(r => r.data),

  bulkSave: (data: BulkSaveAssessmentRequest) =>
    axiosClient.post<EmployeeAssessmentDto[]>(`${BASE}/assessments/bulk-save`, data).then(r => r.data),

  submitAssessment: (id: string) =>
    axiosClient.post(`${BASE}/assessments/${id}/submit`).then(r => r.data),

  bulkSubmit: (data: BulkSubmitRequest) =>
    axiosClient.post(`${BASE}/assessments/bulk-submit`, data).then(r => r.data),

  reopen: (id: string, data: ReopenAssessmentRequest) =>
    axiosClient.post(`${BASE}/assessments/${id}/reopen`, data).then(r => r.data),
};

// ── Dashboard ─────────────────────────────────────────────────────────────────

export const assessmentDashboardApi = {
  getPrimaryLeadDashboard: (groupId: string, periodId?: string) =>
    axiosClient.get<PrimaryLeadDashboardDto>(`${BASE}/dashboard/primary-lead`, { params: { groupId, periodId } }).then(r => r.data),

  getAdminDashboard: (periodId?: string) =>
    axiosClient.get<AdminDashboardDto>(`${BASE}/dashboard/admin`, { params: { periodId } }).then(r => r.data),

  getCoLeadDashboard: (periodId?: string) =>
    axiosClient.get<CoLeadDashboardDto>(`${BASE}/dashboard/co-lead`, { params: { periodId } }).then(r => r.data),

  getExecutiveDashboard: (periodId?: string) =>
    axiosClient.get<ExecutiveDashboardDto>(`${BASE}/dashboard/executive`, { params: { periodId } }).then(r => r.data),
};

// ── Reports ───────────────────────────────────────────────────────────────────

export const assessmentReportApi = {
  getDetailedReport: (filter: Partial<DetailedReportFilter>) =>
    axiosClient.get<PagedResult<DetailedAssessmentReportRow>>(`${BASE}/reports/detailed`, { params: filter }).then(r => r.data),

  getCompletionReport: (periodId?: string, groupId?: string) =>
    axiosClient.get<CompletionReportDto>(`${BASE}/reports/completion`, { params: { periodId, groupId } }).then(r => r.data),

  getGroupDistribution: (periodId: string) =>
    axiosClient.get<GroupDistributionRow[]>(`${BASE}/reports/group-distribution`, { params: { periodId } }).then(r => r.data),

  getRoleDistribution: (periodId: string) =>
    axiosClient.get<RoleDistributionRow[]>(`${BASE}/reports/role-distribution`, { params: { periodId } }).then(r => r.data),

  getEmployeeHistory: (userId: string) =>
    axiosClient.get<EmployeeHistoryRow[]>(`${BASE}/reports/employee-history`, { params: { userId } }).then(r => r.data),

  getTrendReport: (filter: TrendReportFilter) =>
    axiosClient.get<TrendReportDto>(`${BASE}/reports/trend`, { params: filter }).then(r => r.data),

  getRiskReport: (periodId: string) =>
    axiosClient.get<GroupRiskRow[]>(`${BASE}/reports/risk`, { params: { periodId } }).then(r => r.data),

  getImprovementReport: (filter: ImprovementReportFilter) =>
    axiosClient.get<ImprovementRow[]>(`${BASE}/reports/improvement`, { params: filter }).then(r => r.data),

  getWorkRoleRatingReport: (periodId?: string, groupId?: string) =>
    axiosClient.get<WorkRoleRatingRow[]>(`${BASE}/reports/work-role-rating`, { params: { periodId, groupId } }).then(r => r.data),

  exportCsv: (filter: Partial<ExportFilter>) =>
    axiosClient.get<Blob>(`${BASE}/reports/export`, { params: filter, responseType: 'blob' }).then(r => r.data),
};

// ── Audit ─────────────────────────────────────────────────────────────────────

export const assessmentAuditApi = {
  getAuditLogs: (filter: Partial<AuditFilter>) =>
    axiosClient.get<PagedResult<AssessmentAuditLogDto>>(`${BASE}/audit`, { params: filter }).then(r => r.data),
};
// ── Work Roles ────────────────────────────────────────────────────────────

export const workRoleApi = {
  getWorkRoles: (isActive?: boolean) =>
    axiosClient.get<WorkRoleDto[]>(`${BASE}/work-roles`, { params: { isActive } }).then(r => r.data),

  createWorkRole: (data: CreateWorkRoleRequest) =>
    axiosClient.post<WorkRoleDto>(`${BASE}/work-roles`, data).then(r => r.data),

  updateWorkRole: (id: string, data: UpdateWorkRoleRequest) =>
    axiosClient.put<WorkRoleDto>(`${BASE}/work-roles/${id}`, data).then(r => r.data),
};