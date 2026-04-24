using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts.Assessment;

public interface IAssessmentGroupService
{
    Task<PagedResult<AssessmentGroupDto>> GetGroupsAsync(AssessmentGroupFilter filter, CancellationToken ct);
    Task<AssessmentGroupDto>              GetGroupByIdAsync(Guid id, CancellationToken ct);
    Task<AssessmentGroupDto>              CreateGroupAsync(CreateAssessmentGroupRequest request, CancellationToken ct);
    Task<AssessmentGroupDto>              UpdateGroupAsync(Guid id, UpdateAssessmentGroupRequest request, CancellationToken ct);
    Task                                  DeactivateGroupAsync(Guid id, CancellationToken ct);
    Task<List<GroupMemberDto>>            GetGroupMembersAsync(Guid groupId, CancellationToken ct);
    Task                                  AddMemberToGroupAsync(Guid groupId, AssignGroupMemberRequest request, CancellationToken ct);
    Task                                  RemoveMemberFromGroupAsync(Guid groupId, Guid userId, CancellationToken ct);
    Task<List<GroupMemberDto>>            GetGroupCoLeadsAsync(Guid groupId, CancellationToken ct);
    Task                                  AssignCoLeadToGroupAsync(Guid groupId, AssignGroupMemberRequest request, CancellationToken ct);
    Task                                  RemoveCoLeadFromGroupAsync(Guid groupId, Guid userId, CancellationToken ct);
}

public interface IAssessmentPeriodService
{
    Task<PagedResult<AssessmentPeriodDto>> GetPeriodsAsync(AssessmentPeriodFilter filter, CancellationToken ct);
    Task<AssessmentPeriodDto>              GetPeriodByIdAsync(Guid id, CancellationToken ct);
    Task<AssessmentPeriodDto>              CreatePeriodAsync(CreateAssessmentPeriodRequest request, CancellationToken ct);
    Task<AssessmentPeriodDto>              UpdatePeriodAsync(Guid id, UpdateAssessmentPeriodRequest request, CancellationToken ct);
    Task                                   OpenPeriodAsync(Guid id, CancellationToken ct);
    Task                                   ClosePeriodAsync(Guid id, CancellationToken ct);
    Task                                   PublishPeriodAsync(Guid id, CancellationToken ct);
    Task<List<AssessmentPeriodDto>>        GeneratePeriodsAsync(GeneratePeriodsRequest request, CancellationToken ct);
}

public interface IRatingScaleService
{
    Task<List<RatingScaleDto>> GetScalesAsync(CancellationToken ct);
    Task<RatingScaleDto>       CreateScaleAsync(CreateRatingScaleRequest request, CancellationToken ct);
    Task<RatingScaleDto>       UpdateScaleAsync(Guid id, UpdateRatingScaleRequest request, CancellationToken ct);
    Task                       DeactivateScaleAsync(Guid id, CancellationToken ct);
}

public interface IRubricService
{
    Task<List<RubricDefinitionDto>> GetRubricsAsync(string? designationCode, CancellationToken ct);
    Task<List<RubricDefinitionDto>> GetCurrentRubricsForDesignationAsync(string designationCode, CancellationToken ct);
    Task<RubricDefinitionDto>       CreateRubricAsync(CreateRubricRequest request, CancellationToken ct);
    Task<RubricDefinitionDto>       UpdateRubricAsync(Guid id, UpdateRubricRequest request, CancellationToken ct);
}

public interface IParameterService
{
    Task<List<ParameterMasterDto>>      GetParametersAsync(CancellationToken ct);
    Task<ParameterMasterDto>            CreateParameterAsync(CreateParameterRequest request, CancellationToken ct);
    Task<ParameterMasterDto>            UpdateParameterAsync(Guid id, UpdateParameterRequest request, CancellationToken ct);
    Task<List<RoleParameterMappingDto>> GetRoleMappingsAsync(string? designationCode, CancellationToken ct);
    Task<RoleParameterMappingDto>       UpsertRoleMappingAsync(UpsertRoleMappingRequest request, CancellationToken ct);
    Task                                RemoveRoleMappingAsync(Guid id, CancellationToken ct);
}

public interface IEmployeeAssessmentService
{
    Task<PagedResult<EmployeeAssessmentDto>> GetAssessmentsAsync(AssessmentFilter filter, CancellationToken ct);
    Task<EmployeeAssessmentDto>              GetAssessmentByIdAsync(Guid id, CancellationToken ct);
    Task<List<EmployeeAssessmentDto>>        GetOrCreateDraftsForPeriodAsync(AssessmentGridRequest request, CancellationToken ct);
    Task<EmployeeAssessmentDto>              SaveDraftAsync(SaveAssessmentDraftRequest request, CancellationToken ct);
    Task<List<EmployeeAssessmentDto>>        BulkSaveDraftsAsync(BulkSaveAssessmentRequest request, CancellationToken ct);
    Task                                     SubmitAssessmentAsync(Guid id, CancellationToken ct);
    Task                                     BulkSubmitAsync(BulkSubmitRequest request, CancellationToken ct);
    Task                                     ReopenAssessmentAsync(Guid id, ReopenAssessmentRequest request, CancellationToken ct);
    Task<PrimaryLeadDashboardDto>            GetPrimaryLeadDashboardAsync(Guid groupId, Guid? periodId, CancellationToken ct);
    Task<AdminDashboardDto>                  GetAdminDashboardAsync(Guid? periodId, CancellationToken ct);
    Task<CoLeadDashboardDto>                 GetCoLeadDashboardAsync(Guid? periodId, CancellationToken ct);
    Task<ExecutiveDashboardDto>              GetExecutiveDashboardAsync(Guid? periodId, CancellationToken ct);
}

public interface IAssessmentReportService
{
    Task<PagedResult<DetailedAssessmentReportRow>> GetDetailedReportAsync(DetailedReportFilter filter, CancellationToken ct);
    Task<CompletionReportDto>                      GetCompletionReportAsync(Guid? periodId, Guid? groupId, CancellationToken ct);
    Task<List<GroupDistributionRow>>               GetGroupDistributionAsync(Guid periodId, CancellationToken ct);
    Task<List<RoleDistributionRow>>                GetRoleDistributionAsync(Guid periodId, CancellationToken ct);
    Task<List<EmployeeHistoryRow>>                 GetEmployeeHistoryAsync(Guid userId, CancellationToken ct);
    Task<TrendReportDto>                           GetTrendReportAsync(TrendReportFilter filter, CancellationToken ct);
    Task<List<GroupRiskRow>>                       GetRiskReportAsync(Guid periodId, CancellationToken ct);
    Task<List<ImprovementRow>>                     GetImprovementReportAsync(ImprovementReportFilter filter, CancellationToken ct);
    Task<List<WorkRoleRatingRow>>                  GetWorkRoleRatingReportAsync(Guid? periodId, Guid? groupId, CancellationToken ct);
    Task<byte[]>                                   ExportToCsvAsync(ExportFilter filter, CancellationToken ct);
}

public interface IAssessmentAuditService
{
    Task<PagedResult<AssessmentAuditLogDto>> GetAuditLogsAsync(AuditFilter filter, CancellationToken ct);
}

public interface IWorkRoleService
{
    Task<List<WorkRoleDto>> GetWorkRolesAsync(bool? isActive, CancellationToken ct);
    Task<WorkRoleDto>       CreateWorkRoleAsync(CreateWorkRoleRequest request, CancellationToken ct);
    Task<WorkRoleDto>       UpdateWorkRoleAsync(Guid id, UpdateWorkRoleRequest request, CancellationToken ct);
}
