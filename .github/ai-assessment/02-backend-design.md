# AI Assessment Module — Backend Design
## Entities · Service Interfaces · Controllers · Validators · DTOs

> Reference this file when implementing Domain entities, Application services, Infrastructure, and API controllers.

---

## 1. Domain Entities

All entities extend `BaseEntity` (inheriting `Id`, `TenantId`, `CreatedDate`, `CreatedBy`, `ModifiedOn`, `ModifiedBy`, `RecordVersion`).  
File location: `backend/src/KnowHub.Domain/Entities/`

### New Enums
**File**: `backend/src/KnowHub.Domain/Enums/AssessmentEnums.cs`

```csharp
namespace KnowHub.Domain.Enums;

public enum AssessmentPeriodFrequency
{
    Weekly   = 0,
    BiWeekly = 1
}

public enum AssessmentPeriodStatus
{
    Draft     = 0,
    Open      = 1,
    Closed    = 2,
    Published = 3
}

public enum AssessmentStatus
{
    Draft     = 0,
    Submitted = 1,
    Reopened  = 2
}

public enum AssessmentActionType
{
    Created          = 0,
    Updated          = 1,
    Submitted        = 2,
    Reopened         = 3,
    ChampionChanged  = 4,
    CoEAssigned      = 5,
    CoERemoved       = 6,
    EmployeeAssigned = 7,
    EmployeeRemoved  = 8,
    PeriodOpened     = 9,
    PeriodClosed     = 10,
    PeriodPublished  = 11
}
```

---

### 1.1 AIAssessmentGroup
**File**: `backend/src/KnowHub.Domain/Entities/AIAssessmentGroup.cs`

```csharp
public class AIAssessmentGroup : BaseEntity
{
    public string GroupName      { get; set; } = string.Empty;
    public string GroupCode      { get; set; } = string.Empty;
    public string? Description   { get; set; }
    public Guid   ChampionUserId { get; set; }
    public bool   IsActive       { get; set; } = true;

    // Navigation
    public User                                Champion        { get; set; } = null!;
    public ICollection<AIAssessmentGroupEmployee> GroupEmployees { get; set; } = new List<AIAssessmentGroupEmployee>();
    public ICollection<AIAssessmentGroupCoE>     GroupCoEs      { get; set; } = new List<AIAssessmentGroupCoE>();
    public ICollection<EmployeeAssessment>        Assessments    { get; set; } = new List<EmployeeAssessment>();
}
```

### 1.2 AIAssessmentGroupEmployee
**File**: `backend/src/KnowHub.Domain/Entities/AIAssessmentGroupEmployee.cs`

```csharp
public class AIAssessmentGroupEmployee : BaseEntity
{
    public Guid      GroupId       { get; set; }
    public Guid      UserId        { get; set; }
    public DateTime  EffectiveFrom { get; set; }
    public DateTime? EffectiveTo   { get; set; }
    public bool      IsActive      { get; set; } = true;

    // Navigation
    public AIAssessmentGroup Group { get; set; } = null!;
    public User               User  { get; set; } = null!;
}
```

### 1.3 AIAssessmentGroupCoE
**File**: `backend/src/KnowHub.Domain/Entities/AIAssessmentGroupCoE.cs`

```csharp
public class AIAssessmentGroupCoE : BaseEntity
{
    public Guid      GroupId       { get; set; }
    public Guid      UserId        { get; set; }
    public DateTime  EffectiveFrom { get; set; }
    public DateTime? EffectiveTo   { get; set; }
    public bool      IsActive      { get; set; } = true;

    // Navigation
    public AIAssessmentGroup Group { get; set; } = null!;
    public User               User  { get; set; } = null!;
}
```

### 1.4 AssessmentPeriod
**File**: `backend/src/KnowHub.Domain/Entities/AssessmentPeriod.cs`

```csharp
public class AssessmentPeriod : BaseEntity
{
    public string                    Name       { get; set; } = string.Empty;
    public AssessmentPeriodFrequency Frequency  { get; set; }
    public DateOnly                  StartDate  { get; set; }
    public DateOnly                  EndDate    { get; set; }
    public int                       Year       { get; set; }
    public int?                      WeekNumber { get; set; }
    public AssessmentPeriodStatus    Status     { get; set; } = AssessmentPeriodStatus.Draft;
    public bool                      IsActive   { get; set; } = true;

    // Navigation
    public ICollection<EmployeeAssessment> Assessments { get; set; } = new List<EmployeeAssessment>();
}
```

### 1.5 RatingScale
**File**: `backend/src/KnowHub.Domain/Entities/RatingScale.cs`

```csharp
public class RatingScale : BaseEntity
{
    public string Code         { get; set; } = string.Empty;
    public string Name         { get; set; } = string.Empty;
    public int    NumericValue { get; set; }
    public int    DisplayOrder { get; set; }
    public bool   IsActive     { get; set; } = true;
}
```

### 1.6 RubricDefinition
**File**: `backend/src/KnowHub.Domain/Entities/RubricDefinition.cs`

```csharp
public class RubricDefinition : BaseEntity
{
    public string   DesignationCode      { get; set; } = string.Empty;
    public Guid     RatingScaleId        { get; set; }
    public string   BehaviorDescription  { get; set; } = string.Empty;
    public string   ProcessDescription   { get; set; } = string.Empty;
    public string   EvidenceDescription  { get; set; } = string.Empty;
    public int      VersionNo            { get; set; } = 1;
    public DateOnly EffectiveFrom        { get; set; }
    public DateOnly? EffectiveTo         { get; set; }
    public bool     IsActive             { get; set; } = true;

    // Navigation
    public RatingScale RatingScale { get; set; } = null!;
}
```

### 1.7 ParameterMaster
**File**: `backend/src/KnowHub.Domain/Entities/ParameterMaster.cs`

```csharp
public class ParameterMaster : BaseEntity
{
    public string  Name         { get; set; } = string.Empty;
    public string  Code         { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public string  Category     { get; set; } = string.Empty;
    public int     DisplayOrder { get; set; }
    public bool    IsActive     { get; set; } = true;
}
```

### 1.8 RoleParameterMapping
**File**: `backend/src/KnowHub.Domain/Entities/RoleParameterMapping.cs`

```csharp
public class RoleParameterMapping : BaseEntity
{
    public string  DesignationCode { get; set; } = string.Empty;
    public Guid    ParameterId     { get; set; }
    public decimal Weightage       { get; set; }
    public int     DisplayOrder    { get; set; }
    public bool    IsMandatory     { get; set; }
    public bool    IsActive        { get; set; } = true;

    // Navigation
    public ParameterMaster Parameter { get; set; } = null!;
}
```

### 1.9 EmployeeAssessment
**File**: `backend/src/KnowHub.Domain/Entities/EmployeeAssessment.cs`

```csharp
public class EmployeeAssessment : BaseEntity
{
    public Guid             UserId               { get; set; }
    public Guid             GroupId              { get; set; }
    public Guid             AssessmentPeriodId   { get; set; }
    public string           RoleCode             { get; set; } = string.Empty;
    public string?          Designation          { get; set; }
    public Guid             RatingScaleId        { get; set; }
    public int              RatingValue          { get; set; }
    public string?          Comment              { get; set; }
    public string?          EvidenceNotes        { get; set; }
    public string?          ParameterSummaryJson { get; set; }
    public AssessmentStatus Status               { get; set; } = AssessmentStatus.Draft;
    public Guid?            SubmittedBy          { get; set; }
    public DateTime?        SubmittedOn          { get; set; }

    // Navigation
    public User                 Employee                   { get; set; } = null!;
    public AIAssessmentGroup    Group                      { get; set; } = null!;
    public AssessmentPeriod     Period                     { get; set; } = null!;
    public RatingScale          RatingScale                { get; set; } = null!;
    public User?                Submitter                  { get; set; }
    public ICollection<EmployeeAssessmentParameterDetail> ParameterDetails { get; set; }
        = new List<EmployeeAssessmentParameterDetail>();
    public ICollection<AssessmentAuditLog> AuditLogs      { get; set; }
        = new List<AssessmentAuditLog>();
}
```

### 1.10 EmployeeAssessmentParameterDetail
**File**: `backend/src/KnowHub.Domain/Entities/EmployeeAssessmentParameterDetail.cs`

```csharp
public class EmployeeAssessmentParameterDetail : BaseEntity
{
    public Guid    EmployeeAssessmentId   { get; set; }
    public Guid    ParameterId            { get; set; }
    public Guid    ParameterRatingScaleId { get; set; }
    public string? Comment               { get; set; }
    public string? EvidenceNotes         { get; set; }

    // Navigation
    public EmployeeAssessment Assessment     { get; set; } = null!;
    public ParameterMaster    Parameter      { get; set; } = null!;
    public RatingScale        ParameterRating { get; set; } = null!;
}
```

### 1.11 AssessmentAuditLog
**File**: `backend/src/KnowHub.Domain/Entities/AssessmentAuditLog.cs`

```csharp
public class AssessmentAuditLog : BaseEntity
{
    public Guid?                 EmployeeAssessmentId { get; set; }
    public string                RelatedEntityType    { get; set; } = string.Empty;
    public Guid                  RelatedEntityId      { get; set; }
    public AssessmentActionType  ActionType           { get; set; }
    public string?               OldValueJson         { get; set; }
    public string?               NewValueJson         { get; set; }
    public Guid                  ChangedBy            { get; set; }
    public DateTime              ChangedOn            { get; set; }
    public string?               Remarks              { get; set; }

    // Navigation
    public EmployeeAssessment? Assessment { get; set; }
    public User                ChangedByUser { get; set; } = null!;
}
```

---

## 2. EF Core Configurations

Add `DbSet` properties to `KnowHubDbContext.cs`:

```csharp
// AI Assessment Module
public DbSet<AIAssessmentGroup>                    AIAssessmentGroups                    { get; set; }
public DbSet<AIAssessmentGroupEmployee>            AIAssessmentGroupEmployees            { get; set; }
public DbSet<AIAssessmentGroupCoE>                 AIAssessmentGroupCoEs                 { get; set; }
public DbSet<AssessmentPeriod>                     AssessmentPeriods                     { get; set; }
public DbSet<RatingScale>                          RatingScales                          { get; set; }
public DbSet<RubricDefinition>                     RubricDefinitions                     { get; set; }
public DbSet<ParameterMaster>                      ParameterMasters                      { get; set; }
public DbSet<RoleParameterMapping>                 RoleParameterMappings                 { get; set; }
public DbSet<EmployeeAssessment>                   EmployeeAssessments                   { get; set; }
public DbSet<EmployeeAssessmentParameterDetail>    EmployeeAssessmentParameterDetails    { get; set; }
public DbSet<AssessmentAuditLog>                   AssessmentAuditLogs                   { get; set; }
```

Create EF configurations in `backend/src/KnowHub.Infrastructure/Persistence/Configurations/` following existing patterns. Use `IEntityTypeConfiguration<T>` with `HasKey`, `HasIndex`, `HasOne`/`HasMany` relationships. `ParameterSummaryJson`, `OldValueJson`, `NewValueJson` should use `.HasColumnType("jsonb")`.

---

## 3. Application — Service Interfaces

Location: `backend/src/KnowHub.Application/Contracts/`

### IAIAssessmentGroupService
```csharp
public interface IAIAssessmentGroupService
{
    Task<PagedResult<AIAssessmentGroupDto>> GetGroupsAsync(AIAssessmentGroupFilter filter, CancellationToken ct);
    Task<AIAssessmentGroupDto>              GetGroupByIdAsync(Guid id, CancellationToken ct);
    Task<AIAssessmentGroupDto>              CreateGroupAsync(CreateAIAssessmentGroupRequest request, CancellationToken ct);
    Task<AIAssessmentGroupDto>              UpdateGroupAsync(Guid id, UpdateAIAssessmentGroupRequest request, CancellationToken ct);
    Task                                    DeactivateGroupAsync(Guid id, CancellationToken ct);

    Task<List<GroupMemberDto>>              GetGroupEmployeesAsync(Guid groupId, CancellationToken ct);
    Task                                    AddEmployeeToGroupAsync(Guid groupId, AssignGroupMemberRequest request, CancellationToken ct);
    Task                                    RemoveEmployeeFromGroupAsync(Guid groupId, Guid userId, CancellationToken ct);

    Task<List<GroupMemberDto>>              GetGroupCoEsAsync(Guid groupId, CancellationToken ct);
    Task                                    AssignCoEToGroupAsync(Guid groupId, AssignGroupMemberRequest request, CancellationToken ct);
    Task                                    RemoveCoEFromGroupAsync(Guid groupId, Guid userId, CancellationToken ct);
}
```

### IAssessmentPeriodService
```csharp
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
```

### IRatingScaleService
```csharp
public interface IRatingScaleService
{
    Task<List<RatingScaleDto>> GetScalesAsync(CancellationToken ct);
    Task<RatingScaleDto>       CreateScaleAsync(CreateRatingScaleRequest request, CancellationToken ct);
    Task<RatingScaleDto>       UpdateScaleAsync(Guid id, UpdateRatingScaleRequest request, CancellationToken ct);
    Task                       DeactivateScaleAsync(Guid id, CancellationToken ct);
}
```

### IRubricService
```csharp
public interface IRubricService
{
    Task<List<RubricDefinitionDto>> GetRubricsAsync(string? designationCode, CancellationToken ct);
    Task<List<RubricDefinitionDto>> GetCurrentRubricsForDesignationAsync(string designationCode, CancellationToken ct);
    Task<RubricDefinitionDto>       CreateRubricAsync(CreateRubricRequest request, CancellationToken ct);
    Task<RubricDefinitionDto>       UpdateRubricAsync(Guid id, UpdateRubricRequest request, CancellationToken ct);
}
```

### IParameterService
```csharp
public interface IParameterService
{
    Task<List<ParameterMasterDto>>      GetParametersAsync(CancellationToken ct);
    Task<ParameterMasterDto>            CreateParameterAsync(CreateParameterRequest request, CancellationToken ct);
    Task<ParameterMasterDto>            UpdateParameterAsync(Guid id, UpdateParameterRequest request, CancellationToken ct);
    Task<List<RoleParameterMappingDto>> GetRoleMappingsAsync(string? designationCode, CancellationToken ct);
    Task<RoleParameterMappingDto>       UpsertRoleMappingAsync(UpsertRoleMappingRequest request, CancellationToken ct);
    Task                                RemoveRoleMappingAsync(Guid id, CancellationToken ct);
}
```

### IEmployeeAssessmentService
```csharp
public interface IEmployeeAssessmentService
{
    Task<PagedResult<EmployeeAssessmentDto>> GetAssessmentsAsync(AssessmentFilter filter, CancellationToken ct);
    Task<EmployeeAssessmentDto>              GetAssessmentByIdAsync(Guid id, CancellationToken ct);
    Task<EmployeeAssessmentDto>              SaveDraftAsync(SaveAssessmentDraftRequest request, CancellationToken ct);
    Task<List<EmployeeAssessmentDto>>        BulkSaveDraftsAsync(BulkSaveAssessmentRequest request, CancellationToken ct);
    Task                                     SubmitAssessmentAsync(Guid id, CancellationToken ct);
    Task                                     BulkSubmitAsync(BulkSubmitRequest request, CancellationToken ct);
    Task                                     ReopenAssessmentAsync(Guid id, string remarks, CancellationToken ct);

    // Pre-populate grid with all active group employees (creates Draft if none exists)
    Task<List<EmployeeAssessmentDto>>         GetOrCreateDraftsForPeriodAsync(Guid groupId, Guid periodId, CancellationToken ct);

    // Dashboard aggregates
    Task<ChampionDashboardDto>               GetChampionDashboardAsync(Guid groupId, Guid periodId, CancellationToken ct);
    Task<AdminDashboardDto>                  GetAdminDashboardAsync(Guid? periodId, CancellationToken ct);
    Task<CoEDashboardDto>                    GetCoEDashboardAsync(Guid? periodId, CancellationToken ct);
    Task<ExecutiveDashboardDto>              GetExecutiveDashboardAsync(Guid? periodId, CancellationToken ct);
}
```

### IAssessmentReportService
```csharp
public interface IAssessmentReportService
{
    Task<PagedResult<DetailedAssessmentReportRow>>  GetDetailedReportAsync(DetailedReportFilter filter, CancellationToken ct);
    Task<CompletionReportDto>                       GetCompletionReportAsync(CompletionReportFilter filter, CancellationToken ct);
    Task<List<GroupDistributionRow>>                GetGroupDistributionAsync(Guid periodId, CancellationToken ct);
    Task<List<RoleDistributionRow>>                 GetRoleDistributionAsync(Guid periodId, CancellationToken ct);
    Task<List<EmployeeHistoryRow>>                  GetEmployeeHistoryAsync(Guid userId, CancellationToken ct);
    Task<TrendReportDto>                            GetTrendReportAsync(TrendReportFilter filter, CancellationToken ct);
    Task<List<GroupRiskRow>>                        GetRiskReportAsync(Guid periodId, CancellationToken ct);
    Task<List<ImprovementRow>>                      GetImprovementReportAsync(ImprovementReportFilter filter, CancellationToken ct);
    Task<byte[]>                                    ExportToExcelAsync(ExportFilter filter, CancellationToken ct);
    Task<byte[]>                                    ExportToCsvAsync(ExportFilter filter, CancellationToken ct);
}
```

---

## 4. Key DTOs & Request Models

Location: `backend/src/KnowHub.Application/Models/`

### AIAssessmentGroupDto
```csharp
public record AIAssessmentGroupDto(
    Guid    Id,
    string  GroupName,
    string  GroupCode,
    string? Description,
    Guid    ChampionUserId,
    string  ChampionName,
    int     ActiveEmployeeCount,
    bool    IsActive,
    DateTime CreatedDate
);
```

### CreateAIAssessmentGroupRequest
```csharp
public record CreateAIAssessmentGroupRequest(
    string  GroupName,
    string  GroupCode,
    string? Description,
    Guid    ChampionUserId   // dropdown: Users table
);
```

### AssessmentPeriodDto
```csharp
public record AssessmentPeriodDto(
    Guid                     Id,
    string                   Name,
    AssessmentPeriodFrequency Frequency,
    DateOnly                 StartDate,
    DateOnly                 EndDate,
    int                      Year,
    int?                     WeekNumber,
    AssessmentPeriodStatus   Status,
    bool                     IsActive
);
```

### GeneratePeriodsRequest
```csharp
public record GeneratePeriodsRequest(
    int                      Year,
    AssessmentPeriodFrequency Frequency
);
```

### SaveAssessmentDraftRequest
```csharp
public record SaveAssessmentDraftRequest(
    Guid    UserId,           // dropdown: group employees
    Guid    GroupId,
    Guid    AssessmentPeriodId,
    Guid    RatingScaleId,    // dropdown: RatingScales
    int     RatingValue,
    string? Comment,
    string? EvidenceNotes
);
```

### BulkSaveAssessmentRequest
```csharp
public record BulkSaveAssessmentRequest(
    Guid                           AssessmentPeriodId,
    Guid                           GroupId,
    List<SaveAssessmentDraftRequest> Assessments
);
```

### EmployeeAssessmentDto
```csharp
public record EmployeeAssessmentDto(
    Guid             Id,
    Guid             UserId,
    string           EmployeeName,
    string?          Department,
    string?          Designation,
    Guid             GroupId,
    string           GroupName,
    Guid             AssessmentPeriodId,
    string           PeriodName,
    Guid             RatingScaleId,
    string           RatingScaleName,
    int              RatingValue,
    string?          Comment,
    string?          EvidenceNotes,
    AssessmentStatus Status,
    int?             PreviousRatingValue,  // null if no prior period
    string?          PreviousRatingName,
    int?             RatingChange,         // CurrentRating - PreviousRating
    DateTime?        SubmittedOn
);
```

### ChampionDashboardDto
```csharp
public record ChampionDashboardDto(
    int                     TotalEmployees,
    int                     Rated,
    int                     Pending,
    decimal                 CompletionPercent,
    List<RatingBandCount>   CurrentRatingDistribution,
    List<RatingBandCount>   PreviousRatingDistribution,
    string                  PeriodName,
    string                  GroupName
);

public record RatingBandCount(string RatingName, int NumericValue, int Count);
```

### DetailedAssessmentReportRow
```csharp
public record DetailedAssessmentReportRow(
    string  EmployeeName,
    string? Designation,
    string? Department,
    string  GroupName,
    string  ChampionName,
    string? CoEName,
    string  PeriodName,
    int?    PreviousRatingValue,
    string? PreviousRatingName,
    int     CurrentRatingValue,
    string  CurrentRatingName,
    int?    RatingChange,
    string? Comment,
    string? EvidenceNotes,
    string  Status
);
```

---

## 5. Validators

Location: `backend/src/KnowHub.Application/Validators/`

### CreateAIAssessmentGroupRequestValidator
```csharp
// GroupName: NotEmpty, MaxLength(200)
// GroupCode: NotEmpty, MaxLength(50), must be unique per tenant (async db check)
// ChampionUserId: NotEmpty, user must exist in tenant and not lead another active group
```

### SaveAssessmentDraftRequestValidator
```csharp
// UserId: NotEmpty
// GroupId: NotEmpty
// AssessmentPeriodId: NotEmpty, period must have Status = Open
// RatingScaleId: NotEmpty, must be active rating scale in tenant
// RatingValue: InclusiveBetween(0, 4)
// Comment: MaxLength(2000)
// EvidenceNotes: MaxLength(2000)
```

### CreateAssessmentPeriodRequestValidator
```csharp
// Name: NotEmpty, MaxLength(200), unique per tenant
// StartDate: NotEmpty
// EndDate: must be >= StartDate
// Year: must match StartDate.Year
// WeekNumber: optional, 1–53
```

---

## 6. Controllers

Location: `backend/src/KnowHub.Api/Controllers/`  
All under route prefix: `/api/ai-assessment/`  
All require `[Authorize]`.  
All use `ICurrentUserAccessor` for tenant/user isolation.

### AIAssessmentGroupsController
Route: `api/ai-assessment/groups`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/` | All authenticated | List groups (Champion sees own; CoE sees assigned; Admin sees all) |
| GET | `/{id}` | All authenticated | Get group detail |
| POST | `/` | AdminOrAbove | Create group |
| PUT | `/{id}` | AdminOrAbove | Update group |
| DELETE | `/{id}` | AdminOrAbove | Deactivate group |
| GET | `/{id}/employees` | Admin, Champion(own group), CoE(assigned) | List employees |
| POST | `/{id}/employees` | AdminOrAbove | Add employee |
| DELETE | `/{id}/employees/{userId}` | AdminOrAbove | Remove employee |
| GET | `/{id}/coe` | AdminOrAbove | List CoE assignments |
| POST | `/{id}/coe` | AdminOrAbove | Assign CoE |
| DELETE | `/{id}/coe/{userId}` | AdminOrAbove | Remove CoE |

### AssessmentPeriodsController
Route: `api/ai-assessment/periods`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/` | All authenticated | List periods with filters |
| GET | `/{id}` | All authenticated | Get period |
| POST | `/` | AdminOrAbove | Create period |
| PUT | `/{id}` | AdminOrAbove | Update period (only Draft) |
| POST | `/{id}/open` | AdminOrAbove | Draft → Open |
| POST | `/{id}/close` | AdminOrAbove | Open → Closed |
| POST | `/{id}/publish` | AdminOrAbove | Closed → Published |
| POST | `/generate` | AdminOrAbove | Auto-generate periods for year |

### RatingScalesController
Route: `api/ai-assessment/rating-scales`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/` | All authenticated | List all (active only by default) |
| POST | `/` | AdminOrAbove | Create |
| PUT | `/{id}` | AdminOrAbove | Update |
| DELETE | `/{id}` | AdminOrAbove | Deactivate |

### RubricsController
Route: `api/ai-assessment/rubrics`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/` | AdminOrAbove | List with filters (designationCode, ratingScaleId) |
| GET | `/by-designation` | Champion, CoE, AdminOrAbove | Current date-valid rubrics for a designation |
| POST | `/` | AdminOrAbove | Create new rubric version |
| PUT | `/{id}` | AdminOrAbove | Update (creates new version, closes old) |

### ParametersController
Route: `api/ai-assessment/parameters`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/` | AdminOrAbove | List parameters |
| POST | `/` | AdminOrAbove | Create parameter |
| PUT | `/{id}` | AdminOrAbove | Update parameter |
| GET | `/role-mappings` | AdminOrAbove | List role-parameter mappings |
| POST | `/role-mappings` | AdminOrAbove | Upsert role-parameter mapping |
| DELETE | `/role-mappings/{id}` | AdminOrAbove | Remove mapping |

### EmployeeAssessmentsController
Route: `api/ai-assessment/assessments`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/` | Champion(own), CoE(assigned), AdminOrAbove | List with filters |
| GET | `/{id}` | Champion(own), CoE(assigned), AdminOrAbove | Get detail |
| GET | `/grid` | Champion(own group), AdminOrAbove | Pre-populated rating grid (creates Drafts) |
| POST | `/draft` | Champion(own group) | Save single draft |
| POST | `/bulk-save` | Champion(own group) | Bulk save drafts |
| POST | `/{id}/submit` | Champion(own group) | Submit one |
| POST | `/bulk-submit` | Champion(own group) | Submit all Open-period records for group |
| POST | `/{id}/reopen` | AdminOrAbove | Reopen submitted record |

### AIAssessmentDashboardController
Route: `api/ai-assessment/dashboard`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/champion` | Champion | Champion dashboard (own group) |
| GET | `/coe` | CoE | CoE dashboard (assigned groups) |
| GET | `/admin` | AdminOrAbove | Admin/tenant-wide dashboard |
| GET | `/executive` | ManagerOrAbove | Executive summary dashboard |

### AIAssessmentReportsController
Route: `api/ai-assessment/reports`

| Method | Route | Auth | Query params |
|--------|-------|------|-------------|
| GET | `/detailed` | Champion, CoE, Admin | `periodId`, `groupId`, `championId`, `coeId`, `designation`, `department`, `rating`, `status`, `page`, `pageSize` |
| GET | `/completion` | Champion, CoE, Admin | `periodId`, `groupId` |
| GET | `/group-distribution` | Champion, CoE, Admin | `periodId` |
| GET | `/role-distribution` | Admin | `periodId` |
| GET | `/employee-history` | Self or Admin | `userId` |
| GET | `/trend` | Admin | `fromYear`, `toYear`, `frequency` |
| GET | `/risk` | Admin | `periodId` |
| GET | `/improvement` | Admin | `fromPeriodId`, `toPeriodId` |
| GET | `/export/excel` | Champion, CoE, Admin | same as `/detailed` + `reportType` |
| GET | `/export/csv` | Champion, CoE, Admin | same as above |

### AIAssessmentAuditController
Route: `api/ai-assessment/audit`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/` | AdminOrAbove | List audit logs (filterable by `entityId`, `actionType`, `changedBy`, `from`, `to`) |

---

## 7. Authorization Helpers

In each controller, apply these checks (in addition to `[Authorize]`):

**Is Champion of group?**
```csharp
var group = await _db.AIAssessmentGroups
    .FirstOrDefaultAsync(g => g.Id == groupId && g.TenantId == _currentUser.TenantId && g.IsActive);
if (group?.ChampionUserId != _currentUser.UserId && !_currentUser.IsAdminOrAbove)
    return Forbid();
```

**Is CoE of group?**
```csharp
var isCoe = await _db.AIAssessmentGroupCoEs.AnyAsync(c =>
    c.GroupId == groupId && c.UserId == _currentUser.UserId &&
    c.TenantId == _currentUser.TenantId && c.IsActive);
if (!isCoe && !_currentUser.IsAdminOrAbove)
    return Forbid();
```

**Only Open period allowed for write:**
```csharp
var period = await _db.AssessmentPeriods.FindAsync(periodId);
if (period?.Status != AssessmentPeriodStatus.Open)
    throw new BusinessException("Only Open periods allow rating entry.");
```

---

## 8. Service Implementation Pattern

Location: `backend/src/KnowHub.Infrastructure/Services/`  
Register in: `backend/src/KnowHub.Infrastructure/Extensions/ServiceCollectionExtensions.cs`

```csharp
services.AddScoped<IAIAssessmentGroupService, AIAssessmentGroupService>();
services.AddScoped<IAssessmentPeriodService, AssessmentPeriodService>();
services.AddScoped<IRatingScaleService, RatingScaleService>();
services.AddScoped<IRubricService, RubricService>();
services.AddScoped<IParameterService, ParameterService>();
services.AddScoped<IEmployeeAssessmentService, EmployeeAssessmentService>();
services.AddScoped<IAssessmentReportService, AssessmentReportService>();
```

Each implementation:
1. Accepts `KnowHubDbContext` and `ICurrentUserAccessor` via constructor.
2. Always filters by `TenantId = _currentUser.TenantId`.
3. On mutations, sets `CreatedBy`/`ModifiedBy` from `_currentUser.UserId`.
4. Writes `AssessmentAuditLog` on create/update/submit/reopen actions.
5. Uses `AsNoTracking()` on all read queries.

---

## 9. Sample Payloads

### Create Group
```json
POST /api/ai-assessment/groups
{
  "groupName": "Cloud & DevOps Team",
  "groupCode": "CDT-001",
  "description": "Backend infrastructure engineers",
  "championUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### Create Period
```json
POST /api/ai-assessment/periods
{
  "name": "2026-W12",
  "frequency": 0,
  "startDate": "2026-03-16",
  "endDate": "2026-03-22",
  "year": 2026,
  "weekNumber": 12
}
```

### Bulk Save (Champion submits grid)
```json
POST /api/ai-assessment/assessments/bulk-save
{
  "assessmentPeriodId": "aaaa-...",
  "groupId": "bbbb-...",
  "assessments": [
    {
      "userId": "cccc-...",
      "ratingScaleId": "dddd-...",
      "ratingValue": 2,
      "comment": "Consistently uses AI tools for code review",
      "evidenceNotes": "Demonstrated Copilot usage in 3 PRs"
    },
    {
      "userId": "eeee-...",
      "ratingScaleId": "ffff-...",
      "ratingValue": 1,
      "comment": "Still building awareness",
      "evidenceNotes": null
    }
  ]
}
```

### Generate Periods
```json
POST /api/ai-assessment/periods/generate
{
  "year": 2026,
  "frequency": 0
}
// Creates 52 Draft periods named 2026-W01 through 2026-W52
```

### Get Rating Grid
```
GET /api/ai-assessment/assessments/grid?groupId=...&periodId=...
// Returns one EmployeeAssessmentDto per active employee in group.
// Pre-creates Draft records for employees without an assessment for this period.
```
