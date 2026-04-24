using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts.Assessment;

// -- Group DTOs ---------------------------------------------------------------

public class AssessmentGroupDto
{
    public Guid     Id                   { get; init; }
    public string   GroupName            { get; init; } = string.Empty;
    public string   GroupCode            { get; init; } = string.Empty;
    public string?  Description          { get; init; }
    public Guid     PrimaryLeadUserId    { get; init; }
    public string   PrimaryLeadName      { get; init; } = string.Empty;
    public Guid?    CoLeadUserId         { get; init; }
    public string?  CoLeadName           { get; init; }
    public string?  AssessmentCategory   { get; init; }
    public int      ActiveEmployeeCount  { get; init; }
    public bool     IsActive             { get; init; }
    public DateTime CreatedDate          { get; init; }
    public int      RecordVersion        { get; init; }
}

public class CreateAssessmentGroupRequest
{
    public string  GroupName          { get; set; } = string.Empty;
    public string  GroupCode          { get; set; } = string.Empty;
    public string? Description        { get; set; }
    public Guid    PrimaryLeadUserId  { get; set; }
    public Guid?   CoLeadUserId       { get; set; }
    public string? AssessmentCategory { get; set; }
}

public class UpdateAssessmentGroupRequest
{
    public string  GroupName          { get; set; } = string.Empty;
    public string? Description        { get; set; }
    public Guid    PrimaryLeadUserId  { get; set; }
    public Guid?   CoLeadUserId       { get; set; }
    public string? AssessmentCategory { get; set; }
    public bool    IsActive           { get; set; }
    public int     RecordVersion      { get; set; }
}

public class AssignGroupMemberRequest
{
    public Guid  UserId     { get; set; }
    public Guid? WorkRoleId { get; set; }
}

public class GroupMemberDto
{
    public Guid     Id            { get; init; }
    public Guid     UserId        { get; init; }
    public string   FullName      { get; init; } = string.Empty;
    public string?  Designation   { get; init; }
    public string?  Department    { get; init; }
    public string?  Email         { get; init; }
    public Guid?    WorkRoleId    { get; init; }
    public string?  WorkRoleCode  { get; init; }
    public string?  WorkRoleName  { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo  { get; init; }
    public bool     IsActive      { get; init; }
}

// -- WorkRole DTOs -------------------------------------------------------------

public class WorkRoleDto
{
    public Guid   Id            { get; init; }
    public string Code          { get; init; } = string.Empty;
    public string Name          { get; init; } = string.Empty;
    public string Category      { get; init; } = string.Empty;
    public int    DisplayOrder  { get; init; }
    public bool   IsActive      { get; init; }
    public int    RecordVersion { get; init; }
}

public class CreateWorkRoleRequest
{
    public string Code         { get; set; } = string.Empty;
    public string Name         { get; set; } = string.Empty;
    public string Category     { get; set; } = string.Empty;
    public int    DisplayOrder { get; set; }
}

public class UpdateWorkRoleRequest
{
    public string Name          { get; set; } = string.Empty;
    public string Category      { get; set; } = string.Empty;
    public int    DisplayOrder  { get; set; }
    public bool   IsActive      { get; set; }
    public int    RecordVersion { get; set; }
}

// -- Period DTOs ---------------------------------------------------------------

public class AssessmentPeriodDto
{
    public Guid                      Id          { get; init; }
    public string                    Name        { get; init; } = string.Empty;
    public AssessmentPeriodFrequency Frequency   { get; init; }
    public DateOnly                  StartDate   { get; init; }
    public DateOnly                  EndDate     { get; init; }
    public int                       Year        { get; init; }
    public int?                      WeekNumber  { get; init; }
    public AssessmentPeriodStatus    Status      { get; init; }
    public bool                      IsActive    { get; init; }
    public int                       RecordVersion { get; init; }
}

public class CreateAssessmentPeriodRequest
{
    public string                    Name       { get; set; } = string.Empty;
    public AssessmentPeriodFrequency Frequency  { get; set; }
    public DateOnly                  StartDate  { get; set; }
    public DateOnly                  EndDate    { get; set; }
    public int                       Year       { get; set; }
    public int?                      WeekNumber { get; set; }
}

public class UpdateAssessmentPeriodRequest
{
    public string   Name          { get; set; } = string.Empty;
    public DateOnly StartDate     { get; set; }
    public DateOnly EndDate       { get; set; }
    public int?     WeekNumber    { get; set; }
    public int      RecordVersion { get; set; }
}

public class GeneratePeriodsRequest
{
    public int                       Year      { get; set; }
    public AssessmentPeriodFrequency Frequency { get; set; }
}

// -- Rating Scale DTOs ---------------------------------------------------------

public class RatingScaleDto
{
    public Guid   Id           { get; init; }
    public string Code         { get; init; } = string.Empty;
    public string Name         { get; init; } = string.Empty;
    public int    NumericValue { get; init; }
    public int    DisplayOrder { get; init; }
    public bool   IsActive     { get; init; }
    public int    RecordVersion { get; init; }
}

public class CreateRatingScaleRequest
{
    public string Code         { get; set; } = string.Empty;
    public string Name         { get; set; } = string.Empty;
    public int    NumericValue { get; set; }
    public int    DisplayOrder { get; set; }
}

public class UpdateRatingScaleRequest
{
    public string Code         { get; set; } = string.Empty;
    public string Name         { get; set; } = string.Empty;
    public int    NumericValue { get; set; }
    public int    DisplayOrder { get; set; }
    public bool   IsActive     { get; set; }
    public int    RecordVersion { get; set; }
}

// -- Rubric DTOs ---------------------------------------------------------------

public class RubricDefinitionDto
{
    public Guid     Id                   { get; init; }
    public string   DesignationCode      { get; init; } = string.Empty;
    public Guid     RatingScaleId        { get; init; }
    public string   RatingScaleName      { get; init; } = string.Empty;
    public int      RatingScaleValue     { get; init; }
    public string   BehaviorDescription  { get; init; } = string.Empty;
    public string   ProcessDescription   { get; init; } = string.Empty;
    public string   EvidenceDescription  { get; init; } = string.Empty;
    public int      VersionNo            { get; init; }
    public DateOnly EffectiveFrom        { get; init; }
    public DateOnly? EffectiveTo         { get; init; }
    public bool     IsActive             { get; init; }
}

public class CreateRubricRequest
{
    public string    DesignationCode     { get; set; } = string.Empty;
    public Guid      RatingScaleId       { get; set; }
    public string    BehaviorDescription { get; set; } = string.Empty;
    public string    ProcessDescription  { get; set; } = string.Empty;
    public string    EvidenceDescription { get; set; } = string.Empty;
    public DateOnly  EffectiveFrom       { get; set; }
    public DateOnly? EffectiveTo         { get; set; }
}

public class UpdateRubricRequest
{
    public string    BehaviorDescription { get; set; } = string.Empty;
    public string    ProcessDescription  { get; set; } = string.Empty;
    public string    EvidenceDescription { get; set; } = string.Empty;
    public DateOnly  EffectiveFrom       { get; set; }
    public DateOnly? EffectiveTo         { get; set; }
}

// -- Parameter DTOs ------------------------------------------------------------

public class ParameterMasterDto
{
    public Guid    Id           { get; init; }
    public string  Name         { get; init; } = string.Empty;
    public string  Code         { get; init; } = string.Empty;
    public string? Description  { get; init; }
    public string  Category     { get; init; } = string.Empty;
    public int     DisplayOrder { get; init; }
    public bool    IsActive     { get; init; }
    public int     RecordVersion { get; init; }
}

public class CreateParameterRequest
{
    public string  Name         { get; set; } = string.Empty;
    public string  Code         { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public string  Category     { get; set; } = string.Empty;
    public int     DisplayOrder { get; set; }
}

public class UpdateParameterRequest
{
    public string  Name         { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public string  Category     { get; set; } = string.Empty;
    public int     DisplayOrder { get; set; }
    public bool    IsActive     { get; set; }
    public int     RecordVersion { get; set; }
}

public class RoleParameterMappingDto
{
    public Guid    Id              { get; init; }
    public string  DesignationCode { get; init; } = string.Empty;
    public Guid    ParameterId     { get; init; }
    public string  ParameterName   { get; init; } = string.Empty;
    public decimal Weightage       { get; init; }
    public int     DisplayOrder    { get; init; }
    public bool    IsMandatory     { get; init; }
    public bool    IsActive        { get; init; }
    public int     RecordVersion   { get; init; }
}

public class UpsertRoleMappingRequest
{
    public string  DesignationCode { get; set; } = string.Empty;
    public Guid    ParameterId     { get; set; }
    public decimal Weightage       { get; set; }
    public int     DisplayOrder    { get; set; }
    public bool    IsMandatory     { get; set; }
}

// -- Assessment DTOs -----------------------------------------------------------

public class EmployeeAssessmentDto
{
    public Guid             Id                  { get; init; }
    public Guid             UserId              { get; init; }
    public string           EmployeeName        { get; init; } = string.Empty;
    public string?          Department          { get; init; }
    public string?          Designation         { get; init; }
    public Guid             GroupId             { get; init; }
    public string           GroupName           { get; init; } = string.Empty;
    public Guid             AssessmentPeriodId  { get; init; }
    public string           PeriodName          { get; init; } = string.Empty;
    public Guid             RatingScaleId       { get; init; }
    public string           RatingScaleName     { get; init; } = string.Empty;
    public int              RatingValue         { get; init; }
    public string?          Comment             { get; init; }
    public string?          EvidenceNotes       { get; init; }
    public AssessmentStatus Status              { get; init; }
    public int?             PreviousRatingValue { get; init; }
    public string?          PreviousRatingName  { get; init; }
    public int?             RatingChange        { get; init; }
    public DateTime?        SubmittedOn         { get; init; }
    public int              RecordVersion       { get; init; }
}

public class SaveAssessmentDraftRequest
{
    public Guid    UserId             { get; set; }
    public Guid    GroupId            { get; set; }
    public Guid    AssessmentPeriodId { get; set; }
    public Guid    RatingScaleId      { get; set; }
    public int     RatingValue        { get; set; }
    public string? Comment            { get; set; }
    public string? EvidenceNotes      { get; set; }
}

public class BulkSaveAssessmentRequest
{
    public Guid                           AssessmentPeriodId { get; set; }
    public Guid                           GroupId            { get; set; }
    public List<SaveAssessmentDraftRequest> Assessments      { get; set; } = new();
}

public class BulkSubmitRequest
{
    public Guid GroupId  { get; set; }
    public Guid PeriodId { get; set; }
}

public class ReopenAssessmentRequest
{
    public string Remarks { get; set; } = string.Empty;
}

// -- Filters -------------------------------------------------------------------

public class AssessmentGroupFilter
{
    public string? SearchTerm   { get; set; }
    public bool?   IsActive     { get; set; }
    public Guid?   PrimaryLeadId { get; set; }
    public Guid?   CoLeadId     { get; set; }
    public int     PageNumber   { get; set; } = 1;
    public int     PageSize     { get; set; } = 20;
}

public class AssessmentPeriodFilter
{
    public int?  Year       { get; set; }
    public int?  Status     { get; set; }
    public int?  Frequency  { get; set; }
    public int   PageNumber { get; set; } = 1;
    public int   PageSize   { get; set; } = 50;
}

public class AssessmentFilter
{
    public Guid?  GroupId    { get; set; }
    public Guid?  PeriodId   { get; set; }
    public int?   Status     { get; set; }
    public string? Search    { get; set; }
    public int    PageNumber { get; set; } = 1;
    public int    PageSize   { get; set; } = 50;
}

public class AssessmentGridRequest
{
    public Guid  GroupId  { get; set; }
    public Guid  PeriodId { get; set; }
    public string? Search { get; set; }
    public int?  Rating   { get; set; }
    public int?  Status   { get; set; }
}

// -- Dashboard DTOs ------------------------------------------------------------

public class RatingBandCount
{
    public string RatingName   { get; init; } = string.Empty;
    public int    NumericValue { get; init; }
    public int    Count        { get; init; }
}

public class PrimaryLeadDashboardDto
{
    public int                  TotalEmployees               { get; init; }
    public int                  Rated                        { get; init; }
    public int                  Pending                      { get; init; }
    public decimal              CompletionPercent            { get; init; }
    public List<RatingBandCount> CurrentRatingDistribution  { get; init; } = new();
    public List<RatingBandCount> PreviousRatingDistribution { get; init; } = new();
    public string               PeriodName                   { get; init; } = string.Empty;
    public string               GroupName                    { get; init; } = string.Empty;
}

public class GroupCompletionRow
{
    public Guid    GroupId           { get; init; }
    public string  GroupName         { get; init; } = string.Empty;
    public string  PrimaryLeadName   { get; init; } = string.Empty;
    public int     TotalEmployees    { get; init; }
    public int     Submitted         { get; init; }
    public int     Pending           { get; init; }
    public decimal CompletionPercent { get; init; }
}

public class AdminDashboardDto
{
    public int                     TotalGroups              { get; init; }
    public int                     TotalEmployees           { get; init; }
    public decimal                 AvgCompletionPercent     { get; init; }
    public string                  PeriodName               { get; init; } = string.Empty;
    public List<GroupCompletionRow> GroupCompletions        { get; init; } = new();
    public List<RatingBandCount>   RatingDistribution       { get; init; } = new();
}

public class CoLeadDashboardDto
{
    public List<GroupCompletionRow> Groups             { get; init; } = new();
    public List<RatingBandCount>   RatingDistribution  { get; init; } = new();
    public string                  PeriodName          { get; init; } = string.Empty;
}

public class ExecutiveDashboardDto
{
    public decimal               OrgMaturityScore     { get; init; }
    public int                   TotalAssessed        { get; init; }
    public string                PeriodName           { get; init; } = string.Empty;
    public List<RatingBandCount> RatingDistribution   { get; init; } = new();
    public List<GroupTrendRow>   TopImprovingGroups   { get; init; } = new();
    public List<GroupTrendRow>   LaggingGroups        { get; init; } = new();
    public List<PeriodTrendRow>  HistoricalTrend      { get; init; } = new();
}

public class GroupTrendRow
{
    public string  GroupName     { get; init; } = string.Empty;
    public decimal CurrentScore  { get; init; }
    public decimal? Change       { get; init; }
}

public class PeriodTrendRow
{
    public string  PeriodName  { get; init; } = string.Empty;
    public decimal AvgScore    { get; init; }
    public int     TotalRated  { get; init; }
}

// -- Report DTOs ---------------------------------------------------------------

public class DetailedReportFilter
{
    public Guid?   PeriodId      { get; set; }
    public Guid?   GroupId       { get; set; }
    public Guid?   PrimaryLeadId { get; set; }
    public Guid?   CoLeadId      { get; set; }
    public string? Designation   { get; set; }
    public string? Department    { get; set; }
    public int?    Rating        { get; set; }
    public int?    Status        { get; set; }
    public string? Search        { get; set; }
    public int     PageNumber    { get; set; } = 1;
    public int     PageSize      { get; set; } = 50;
}

public class DetailedAssessmentReportRow
{
    public string  EmployeeName        { get; init; } = string.Empty;
    public string? Designation         { get; init; }
    public string? Department          { get; init; }
    public string  GroupName           { get; init; } = string.Empty;
    public string  PrimaryLeadName     { get; init; } = string.Empty;
    public string? CoLeadName          { get; init; }
    public string  PeriodName          { get; init; } = string.Empty;
    public int?    PreviousRatingValue { get; init; }
    public string? PreviousRatingName  { get; init; }
    public int     CurrentRatingValue  { get; init; }
    public string  CurrentRatingName   { get; init; } = string.Empty;
    public int?    RatingChange        { get; init; }
    public string? Comment             { get; init; }
    public string? EvidenceNotes       { get; init; }
    public string  Status              { get; init; } = string.Empty;
}

public class CompletionReportDto
{
    public int                     TotalExpected  { get; init; }
    public int                     Completed      { get; init; }
    public int                     Pending        { get; init; }
    public decimal                 CompletionPct  { get; init; }
    public List<GroupCompletionRow> ByGroup       { get; init; } = new();
}

public class GroupDistributionRow
{
    public string               GroupName     { get; init; } = string.Empty;
    public List<RatingBandCount> Distribution { get; init; } = new();
}

public class RoleDistributionRow
{
    public string               Designation   { get; init; } = string.Empty;
    public decimal              AvgScore      { get; init; }
    public List<RatingBandCount> Distribution { get; init; } = new();
}

public class EmployeeHistoryRow
{
    public string  PeriodName   { get; init; } = string.Empty;
    public int     RatingValue  { get; init; }
    public string  RatingName   { get; init; } = string.Empty;
    public int?    Change       { get; init; }
    public string? Comment      { get; init; }
    public string  Status       { get; init; } = string.Empty;
    public DateOnly StartDate   { get; init; }
}

public class TrendReportFilter
{
    public int  FromYear    { get; set; }
    public int  ToYear      { get; set; }
    public Guid? GroupId    { get; set; }
}

public class TrendReportDto
{
    public List<PeriodTrendRow>   Periods                  { get; init; } = new();
    public int                    ImprovingCount           { get; init; }
    public int                    DecliningCount           { get; init; }
    public int                    StableCount              { get; init; }
}

public class GroupRiskRow
{
    public string  GroupName    { get; init; } = string.Empty;
    public decimal AvgScore     { get; init; }
    public int     Missing      { get; init; }
    public string  RiskLevel    { get; init; } = string.Empty;
}

public class ImprovementReportFilter
{
    public Guid FromPeriodId { get; set; }
    public Guid ToPeriodId   { get; set; }
}

public class ImprovementRow
{
    public string  EmployeeName  { get; init; } = string.Empty;
    public string  GroupName     { get; init; } = string.Empty;
    public int     FromRating    { get; init; }
    public int     ToRating      { get; init; }
    public int     Change        { get; init; }
}

public class WorkRoleRatingRow
{
    public string               WorkRoleCode  { get; init; } = string.Empty;
    public string               WorkRoleName  { get; init; } = string.Empty;
    public string               Category      { get; init; } = string.Empty;
    public int                  TotalRated    { get; init; }
    public decimal              AvgScore      { get; init; }
    public List<RatingBandCount> Distribution { get; init; } = new();
}

public class ExportFilter
{
    public Guid?   PeriodId    { get; set; }
    public Guid?   GroupId     { get; set; }
    public string? Designation { get; set; }
    public string? Department  { get; set; }
    public int?    Rating      { get; set; }
    public string  ReportType  { get; set; } = "Detailed";
}

// -- Audit DTOs ----------------------------------------------------------------

public class AssessmentAuditLogDto
{
    public Guid    Id                    { get; init; }
    public Guid?   EmployeeAssessmentId  { get; init; }
    public string  RelatedEntityType     { get; init; } = string.Empty;
    public Guid    RelatedEntityId       { get; init; }
    public string  ActionType            { get; init; } = string.Empty;
    public string? OldValueJson          { get; init; }
    public string? NewValueJson          { get; init; }
    public Guid    ChangedBy             { get; init; }
    public string  ChangedByName         { get; init; } = string.Empty;
    public DateTime ChangedOn            { get; init; }
    public string? Remarks               { get; init; }
}

public class AuditFilter
{
    public Guid?    EntityId       { get; set; }
    public int?     ActionType     { get; set; }
    public Guid?    ChangedBy      { get; set; }
    public string?  ChangedByName  { get; set; }
    public DateTime? From          { get; set; }
    public DateTime? To            { get; set; }
    public int      PageNumber     { get; set; } = 1;
    public int      PageSize       { get; set; } = 50;
}
