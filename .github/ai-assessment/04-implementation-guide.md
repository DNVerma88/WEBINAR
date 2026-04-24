# AI Assessment Module — Implementation Guide
## Step-by-step instructions for the Feature Orchestrator agent

> This file is the primary execution guide. Follow steps in order. Do not skip ahead.  
> Reference `00-plan-overview.md`, `01-database-schema.md`, `02-backend-design.md`, `03-frontend-design.md`, `05-seed-and-acceptance.md` throughout.

---

## Pre-flight Checks

Before starting:
1. Read all 5 plan files in `.github/ai-assessment/`.
2. Verify the existing `KnowHubDbContext.cs` location by searching for `DbSet<User>`.
3. Verify the existing `ServiceCollectionExtensions.cs` DI registration file.
4. Verify the existing `BaseEntity.cs` pattern.
5. Verify the existing `ICurrentUserAccessor` interface.
6. Confirm the existing SQL migration files in `database/sql/` (should be 001–005).

---

## Phase 1 — Database & Domain Layer

### Step 1.1 — New Enums

Create file `backend/src/KnowHub.Domain/Enums/AssessmentEnums.cs` with all 4 enums:
- `AssessmentPeriodFrequency` (Weekly=0, BiWeekly=1)
- `AssessmentPeriodStatus` (Draft=0, Open=1, Closed=2, Published=3)
- `AssessmentStatus` (Draft=0, Submitted=1, Reopened=2)
- `AssessmentActionType` (12 values, see `02-backend-design.md`)

### Step 1.2 — Domain Entities

Create 11 entity files in `backend/src/KnowHub.Domain/Entities/`:
1. `AIAssessmentGroup.cs`
2. `AIAssessmentGroupEmployee.cs`
3. `AIAssessmentGroupCoE.cs`
4. `AssessmentPeriod.cs`
5. `RatingScale.cs`
6. `RubricDefinition.cs`
7. `ParameterMaster.cs`
8. `RoleParameterMapping.cs`
9. `EmployeeAssessment.cs`
10. `EmployeeAssessmentParameterDetail.cs`
11. `AssessmentAuditLog.cs`

See full entity definitions in `02-backend-design.md` Section 1.

### Step 1.3 — SQL Migration

Create `database/sql/006_AIAssessmentModule.sql` using the exact SQL from `01-database-schema.md`. This contains:
- `AIAssessmentGroups` table + indexes
- `AIAssessmentGroupEmployees` table + indexes
- `AIAssessmentGroupCoEs` table + indexes
- `AssessmentPeriods` table + indexes
- `RatingScales` table + indexes
- `RubricDefinitions` table + indexes
- `ParameterMasters` table + indexes
- `RoleParameterMappings` table + indexes
- `EmployeeAssessments` table + indexes
- `EmployeeAssessmentParameterDetails` table + indexes
- `AssessmentAuditLogs` table + indexes

**Then append the seed data SQL from `05-seed-and-acceptance.md`** (rating scale defaults, sample parameters).

### Step 1.4 — EF Core Configurations

Create 11 configuration files in `backend/src/KnowHub.Infrastructure/Persistence/Configurations/`:

**Pattern to follow**: Copy the closest existing configuration (e.g., `CommunityMemberConfiguration.cs`) and adapt.

Key rules for ALL configs:
- Table name matches plural entity name (e.g., `"AIAssessmentGroups"`)
- All string properties get `HasMaxLength()`
- JSON columns (`ParameterSummaryJson`, `OldValueJson`, `NewValueJson`) use `.HasColumnType("jsonb")`
- `DateOnly` properties use `.HasColumnType("date")`
- `EffectiveFrom`, `EffectiveTo` use `.HasColumnType("date")`
- All navigations configured with correct `DeleteBehavior` (RESTRICT for most, CASCADE for owned collections)

**AIAssessmentGroupConfiguration** key rules:
```csharp
builder.HasIndex(x => new { x.TenantId, x.GroupCode }).IsUnique();
// Unique partial index for champion uniqueness requires raw SQL in OnModelCreating:
modelBuilder.Entity<AIAssessmentGroup>()
    .HasIndex("TenantId", "ChampionUserId")
    .IsUnique()
    .HasFilter("\"IsActive\" = TRUE")
    .HasDatabaseName("IX_AIAssessmentGroups_TenantId_ChampionUserId_Active");
```

**EmployeeAssessmentConfiguration** key rules:
```csharp
builder.HasIndex(x => new { x.TenantId, x.UserId, x.AssessmentPeriodId }).IsUnique();
builder.Property(x => x.ParameterSummaryJson).HasColumnType("jsonb");
```

**AssessmentAuditLogConfiguration** key rules:
```csharp
builder.Property(x => x.OldValueJson).HasColumnType("jsonb");
builder.Property(x => x.NewValueJson).HasColumnType("jsonb");
// EmployeeAssessmentId is nullable FK — HasDeleteBehavior(DeleteBehavior.SetNull)
```

### Step 1.5 — DbContext Update

In `KnowHubDbContext.cs`, add 11 new `DbSet<>` properties (see `02-backend-design.md` Section 2). Place them in a clearly commented region: `// ── AI Assessment Module ──`.

---

## Phase 2 — Application Layer (Services & DTOs)

### Step 2.1 — DTOs / Models

Create directory `backend/src/KnowHub.Application/Models/AIAssessment/`

Create these model files:
- `AIAssessmentGroupDto.cs` — Group list/detail DTO
- `AssessmentPeriodDto.cs` — Period DTO
- `RatingScaleDto.cs` — Scale DTO
- `RubricDefinitionDto.cs` — Rubric DTO
- `ParameterMasterDto.cs` — Parameter DTO
- `RoleParameterMappingDto.cs` — Mapping DTO
- `EmployeeAssessmentDto.cs` — Assessment DTO (includes PreviousRating, RatingChange)
- `GroupMemberDto.cs` — Shared DTO for employee and CoE member lists
- `DashboardDtos.cs` — ChampionDashboardDto, AdminDashboardDto, CoEDashboardDto, ExecutiveDashboardDto, RatingBandCount
- `ReportDtos.cs` — DetailedAssessmentReportRow, CompletionReportDto, GroupDistributionRow, RoleDistributionRow, EmployeeHistoryRow, TrendReportDto, GroupRiskRow, ImprovementRow

### Step 2.2 — Request Models

Create `backend/src/KnowHub.Application/Models/AIAssessment/Requests/`

- `CreateAIAssessmentGroupRequest.cs`
- `UpdateAIAssessmentGroupRequest.cs`
- `AssignGroupMemberRequest.cs` — `record(Guid UserId)`
- `CreateAssessmentPeriodRequest.cs`
- `UpdateAssessmentPeriodRequest.cs`
- `GeneratePeriodsRequest.cs`
- `CreateRatingScaleRequest.cs`
- `UpdateRatingScaleRequest.cs`
- `CreateRubricRequest.cs`
- `UpdateRubricRequest.cs`
- `CreateParameterRequest.cs`
- `UpsertRoleMappingRequest.cs`
- `SaveAssessmentDraftRequest.cs`
- `BulkSaveAssessmentRequest.cs`
- `BulkSubmitRequest.cs` — `record(Guid GroupId, Guid PeriodId)`
- `ReopenAssessmentRequest.cs` — `record(string Remarks)`
- Filter records: `AIAssessmentGroupFilter`, `AssessmentPeriodFilter`, `AssessmentFilter`, `DetailedReportFilter`, `CompletionReportFilter`, `TrendReportFilter`, `ImprovementReportFilter`, `ExportFilter`

### Step 2.3 — Service Interfaces

Create `backend/src/KnowHub.Application/Contracts/` files:
- `IAIAssessmentGroupService.cs`
- `IAssessmentPeriodService.cs`
- `IRatingScaleService.cs`
- `IRubricService.cs`
- `IParameterService.cs`
- `IEmployeeAssessmentService.cs`
- `IAssessmentReportService.cs`

Full method signatures in `02-backend-design.md` Section 3.

### Step 2.4 — Validators

Create `backend/src/KnowHub.Application/Validators/AIAssessment/`

- `CreateAIAssessmentGroupRequestValidator.cs`
- `UpdateAIAssessmentGroupRequestValidator.cs`
- `CreateAssessmentPeriodRequestValidator.cs`
- `SaveAssessmentDraftRequestValidator.cs`
- `CreateRatingScaleRequestValidator.cs`
- `CreateRubricRequestValidator.cs`

Registration: Validators are auto-discovered by the existing DI setup (FluentValidation `RegisterValidatorsFromAssemblyContaining<>`). No manual registration needed.

---

## Phase 3 — Infrastructure Layer (Service Implementations)

Create `backend/src/KnowHub.Infrastructure/Services/AIAssessment/`

### Step 3.1 — AIAssessmentGroupService

Key implementation notes:
- `CreateGroupAsync`: Validate GroupCode uniqueness. Check Champion not leading another active group. Use `throw new ConflictException(...)` for violations (same pattern as existing code).
- `AddEmployeeToGroupAsync`: Check employee not already active in another group. Set `EffectiveFrom = DateTime.UtcNow`. Write `AssessmentAuditLog` with `ActionType = EmployeeAssigned`.
- `RemoveEmployeeFromGroupAsync`: Find active membership, set `EffectiveTo = DateTime.UtcNow`, `IsActive = false`. Write audit log with `ActionType = EmployeeRemoved`.
- Same pattern for CoE assignments.
- On `UpdateGroupAsync` with ChampionUserId changed: write `AssessmentAuditLog` with `ActionType = ChampionChanged`.

### Step 3.2 — AssessmentPeriodService

Key implementation notes:
- `OpenPeriodAsync`: Validates current status = Draft. Transition → Open. Write audit log `PeriodOpened`.
- `ClosePeriodAsync`: Validates status = Open. Transition → Closed. Write audit log `PeriodClosed`.
- `PublishPeriodAsync`: Validates status = Closed. Transition → Published. Write audit log `PeriodPublished`.
- `GeneratePeriodsAsync`: For Weekly → calculate ISO week start dates for all 52 weeks of the year. For BiWeekly → calculate 26 bi-weekly periods. Create all as `Draft`. Skip weeks that already exist (idempotent).

ISO week calculation helper:
```csharp
private static DateOnly GetISOWeekStartDate(int year, int week)
{
    var jan4 = new DateOnly(year, 1, 4);
    int daysOffset = DayOfWeek.Monday - jan4.DayOfWeek;
    var firstMonday = jan4.AddDays(daysOffset);
    return firstMonday.AddDays((week - 1) * 7);
}
```

### Step 3.3 — RatingScaleService

Simple CRUD. Protect against deactivating the last active scale.

### Step 3.4 — RubricService

Key implementation notes:
- `UpdateRubricAsync`: Do NOT update in place. Instead:
  1. Set `EffectiveTo = today - 1 day` on the existing record, `IsActive = false`.
  2. Insert new record with `VersionNo = old.VersionNo + 1`, `EffectiveFrom = today`.
  This preserves rubric history at assessment time.
- `GetCurrentRubricsForDesignationAsync`: Filter `IsActive = true AND EffectiveFrom <= today AND (EffectiveTo IS NULL OR EffectiveTo >= today)`.

### Step 3.5 — EmployeeAssessmentService

Key implementation notes:

**`GetOrCreateDraftsForPeriodAsync`**:
1. Get all active employees in the group via `AIAssessmentGroupEmployees`.
2. Get existing assessments for (groupId, periodId).
3. For each employee without an existing assessment, create a Draft record with default `RatingValue = 0`.
4. Return all assessments (existing + newly created).

**`SaveDraftAsync`**:
1. Check period status = Open. Throw `BusinessException` if not.
2. Upsert: find existing (UserId, PeriodId) or create new.
3. If status = Submitted, only Admin can overwrite (via Reopen first).
4. Capture `RoleCode` = employee's `User.Designation ?? "Unknown"` at save time (snapshot).
5. Write audit log `Created` or `Updated`.

**`BulkSaveDraftsAsync`**:
- Verify all records belong to the Champion's group.
- Validate period is Open.
- Call `SaveDraftAsync` for each in a single transaction scope.

**`SubmitAssessmentAsync`**:
1. Verify period = Open.
2. Verify `Status = Draft` or `Status = Reopened`.
3. Set `Status = Submitted`, `SubmittedBy = currentUserId`, `SubmittedOn = UtcNow`.
4. Write audit log `Submitted`.

**`BulkSubmitAsync`**:
- Load all `Draft` and `Reopened` assessments for (GroupId, PeriodId) where Champion owns the group.
- Submit each in a loop. Wrap in transaction.

**`ReopenAssessmentAsync`**: AdminOrAbove only. Set `Status = Reopened`. Write audit log `Reopened` with Remarks.

**Previous rating calculation**:
- When building `EmployeeAssessmentDto`, find the most recent prior completed assessment for the same UserId with status = Submitted, ordered by period StartDate DESC.
- `PreviousRatingValue` = that assessment's `RatingValue` (null if none).
- `RatingChange` = `CurrentRatingValue - PreviousRatingValue` (null if no previous).

**Dashboard queries** — use direct EF LINQ projections for aggregates, not loading full entity graphs.

### Step 3.6 — AssessmentReportService

Key queries:

**Detailed Report**: Join EmployeeAssessments → Users → AIAssessmentGroups → AIAssessmentGroupCoEs → AssessmentPeriods. Support all filters. Return paged results.

**Completion Report**:
```
SELECT g.GroupName, u.FullName as ChampionName,
  COUNT(e.Id) as Total,
  SUM(CASE WHEN e.Status = 1 THEN 1 ELSE 0 END) as Submitted,
  COUNT(e.Id) - SUM(CASE WHEN e.Status = 1 THEN 1 ELSE 0 END) as Pending
FROM EmployeeAssessments e
JOIN AIAssessmentGroups g ON e.GroupId = g.Id
JOIN Users u ON g.ChampionUserId = u.Id
WHERE e.AssessmentPeriodId = @periodId AND e.TenantId = @tenantId
GROUP BY g.Id, g.GroupName, u.FullName
```

**Trend**: Group assessments by period, calculate avg(RatingValue) and distribution counts per period.

**Employee History**: All assessments for UserId, ordered by period StartDate, with Change calculated in-application.

**Export**: Use `ClosedXML` or `NPOI` NuGet package for Excel export. CSV uses `StringBuilder`. Check if `ClosedXML` is already in the project; if not, add via NuGet.

### Step 3.7 — DI Registration

In `ServiceCollectionExtensions.cs`, add:
```csharp
services.AddScoped<IAIAssessmentGroupService, AIAssessmentGroupService>();
services.AddScoped<IAssessmentPeriodService, AssessmentPeriodService>();
services.AddScoped<IRatingScaleService, RatingScaleService>();
services.AddScoped<IRubricService, RubricService>();
services.AddScoped<IParameterService, ParameterService>();
services.AddScoped<IEmployeeAssessmentService, EmployeeAssessmentService>();
services.AddScoped<IAssessmentReportService, AssessmentReportService>();
```

---

## Phase 4 — API Controllers

Create 7 controller files in `backend/src/KnowHub.Api/Controllers/`:

1. `AIAssessmentGroupsController.cs`
2. `AssessmentPeriodsController.cs`
3. `RatingScalesController.cs`
4. `RubricsController.cs`
5. `ParametersController.cs`
6. `EmployeeAssessmentsController.cs`
7. `AIAssessmentDashboardController.cs`
8. `AIAssessmentReportsController.cs`
9. `AIAssessmentAuditController.cs`

**Pattern to follow**: Copy `AnalyticsController.cs` as the structural template. It shows:
- `[Authorize]` at class level
- `ICurrentUserAccessor` injection
- Service injection
- Paged results pattern
- Error handling via global middleware

**Authorization pattern** (add to each controller that has Champion/CoE-gated endpoints):
```csharp
private async Task<bool> IsChampionOfGroupAsync(Guid groupId)
{
    var group = await _db.AIAssessmentGroups
        .AsNoTracking()
        .FirstOrDefaultAsync(g => g.Id == groupId &&
                                  g.TenantId == _currentUser.TenantId &&
                                  g.IsActive);
    return group?.ChampionUserId == _currentUser.UserId;
}

private async Task<bool> IsCoEOfGroupAsync(Guid groupId)
    => await _db.AIAssessmentGroupCoEs.AnyAsync(c =>
        c.GroupId == groupId && c.UserId == _currentUser.UserId &&
        c.TenantId == _currentUser.TenantId && c.IsActive);
```

> **IMPORTANT**: Inject `KnowHubDbContext` directly into controllers ONLY for these lightweight authorization checks. All business logic stays in the service layer.

---

## Phase 5 — Frontend

### Step 5.1 — Navigation

In `frontend/src/components/AppLayout.tsx`:
1. Import `PsychologyIcon` from `@mui/icons-material/Psychology`.
2. Add nav item to the list:
   ```tsx
   { label: 'AI Assessment', path: '/ai-assessment', icon: <PsychologyIcon /> }
   ```
3. Place it after Analytics (or near the end of the nav list).

### Step 5.2 — Routes

In `frontend/src/routes.tsx`:
1. Import `AIAssessmentPage`, `AIAssessmentGroupDetailPage`, `EmployeeAssessmentHistoryPage`.
2. Add 3 routes inside PrivateRoute/AppLayout wrapper:
   ```tsx
   <Route path="/ai-assessment" element={<AIAssessmentPage />} />
   <Route path="/ai-assessment/groups/:id" element={<AIAssessmentGroupDetailPage />} />
   <Route path="/ai-assessment/employees/:userId/history" element={<EmployeeAssessmentHistoryPage />} />
   ```

### Step 5.3 — Feature Folder

Create `frontend/src/features/ai-assessment/` as per `03-frontend-design.md` Section 3.

### Step 5.4 — Types & API Layer

Create `frontend/src/features/ai-assessment/types.ts` (see `03-frontend-design.md` Section 15).
Create `frontend/src/features/ai-assessment/api/aiAssessment.ts` (see `03-frontend-design.md` Section 4).

### Step 5.5 — Role Hook

Create `frontend/src/features/ai-assessment/hooks/useAIAssessmentRole.ts` (see `03-frontend-design.md` Section 18).

### Step 5.6 — Main Page

Create `AIAssessmentPage.tsx` with MUI `<Tabs>` + `<Tab>` pattern (copy from `AnalyticsDashboardPage.tsx` structure). Render `PageHeader` with title "AI Maturity Assessment". Use `useAIAssessmentRole()` to determine visible tabs.

### Step 5.7 — Tab Components

Implement each tab in order of usefulness:

**Priority 1 (Phase 1 MVP)**:
1. `DashboardTab.tsx` — Champion view minimum viable
2. `RateEmployeesTab.tsx` — Core Champion rating entry screen
3. `GroupsTab.tsx` — Admin group management
4. `PeriodsTab.tsx` — Admin period management
5. `RatingScalesTab.tsx` — Admin rating scale management

**Priority 2 (Phase 1 completion)**:
6. `RubricTab.tsx` — Admin rubric management
7. `ParametersTab.tsx` — Admin parameter management

**Priority 3 (Phase 2)**:
8. `ReportsTab.tsx` — All reports
9. `AuditTab.tsx` — Audit log viewer
10. `AIAssessmentGroupDetailPage.tsx` — Group member management
11. `EmployeeAssessmentHistoryPage.tsx` — Employee history

### Step 5.8 — Shared Components

Create these shared components in `frontend/src/features/ai-assessment/components/`:
1. `RatingBadge.tsx` — color-coded rating chip (see `03-frontend-design.md` Section 16)
2. `GroupFormDialog.tsx` — create/edit group
3. `PeriodFormDialog.tsx` — create/edit period
4. `RatingScaleFormDialog.tsx` — create/edit scale
5. `GroupMemberDialog.tsx` — add member to group
6. `RubricPanel.tsx` — rubric display panel
7. `RatingDistributionChart.tsx` — horizontal bar chart
8. `CompletionProgressCard.tsx` — stat card with progress

---

## Phase 6 — Testing

Create test files in `backend/tests/KnowHub.Tests/Services/AIAssessment/`:

1. `AIAssessmentGroupServiceTests.cs`
   - `CreateGroup_Valid_ReturnsGroup()`
   - `CreateGroup_DuplicateCode_ThrowsConflict()`
   - `CreateGroup_ChampionLeadsAnotherGroup_ThrowsConflict()`
   - `AddEmployee_AlreadyInAnotherGroup_ThrowsConflict()`
   - `RemoveEmployee_WhenActive_SetsEffectiveTo()`

2. `AssessmentPeriodServiceTests.cs`
   - `OpenPeriod_WhenDraft_Succeeds()`
   - `OpenPeriod_WhenNotDraft_ThrowsBusinessException()`
   - `GeneratePeriods_Weekly_Creates52Periods()`
   - `GeneratePeriods_Idempotent_DoesNotDuplicate()`

3. `EmployeeAssessmentServiceTests.cs`
   - `SaveDraft_OpenPeriod_SavesSuccessfully()`
   - `SaveDraft_ClosedPeriod_ThrowsBusinessException()`
   - `SubmitAssessment_ValidDraft_SetsStatusSubmitted()`
   - `SubmitAssessment_AlreadySubmitted_ThrowsConflict()`
   - `ReopenAssessment_ByAdmin_SetsStatusReopened()`
   - `GetOrCreateDrafts_PrePopulatesAllGroupMembers()`
   - `BulkSave_SavesAllInTransaction()`

Create `TestHelpers/AIAssessment/FakeAIAssessmentRepository.cs` following existing `FakeXxx` pattern.

---

## Phase 7 — Build & Verify

### Step 7.1 — Build backend
```powershell
cd c:\Webinar
dotnet build KnowHub.slnx
```
Fix all compiler errors before proceeding.

### Step 7.2 — Run tests
```powershell
dotnet test KnowHub.slnx
```
All tests must pass.

### Step 7.3 — Run SQL migration
Apply `database/sql/006_AIAssessmentModule.sql` to the development database:
```powershell
# Use psql or the existing migration runner
psql -h localhost -U postgres -d knowhub_dev -f database/sql/006_AIAssessmentModule.sql
```

### Step 7.4 — Start backend and verify endpoints
```powershell
cd backend/src/KnowHub.Api
dotnet run --launch-profile http
```
Open `http://localhost:5200` and test key endpoints.

### Step 7.5 — Build frontend
```powershell
cd frontend
npm run build
```
Fix all TypeScript errors before finalizing.

### Step 7.6 — Start frontend dev server
```powershell
cd frontend
npm run dev
```
Navigate to `http://localhost:5173/ai-assessment` and verify:
- Navigation entry appears
- Tabs render correctly per role
- Dashboard loads without error
- Rating entry grid displays

---

## Important Conventions (DO NOT DEVIATE)

1. **All column names**: PascalCase, no underscores.
2. **All entities extend `BaseEntity`** (never bare classes).
3. **All queries filter by `TenantId`** from `ICurrentUserAccessor`.
4. **Admin check**: Always use `_currentUser.IsAdminOrAbove` — never `IsInRole(Admin)` alone.
5. **No hardcoded role strings in UI**: Use `isAdminOrAbove = isAdmin || isSuperAdmin`.
6. **FluentValidation**: Create validators for all request models. Never inline validation.
7. **No Moq in tests**: Use `FakeXxx` hand-written helpers only.
8. **EF Core**: Use `AsNoTracking()` on all reads. Use `SaveChangesAsync()` for all writes.
9. **Audit on every write**: Every create/update/state-change writes to `AssessmentAuditLogs`.
10. **Champion uniqueness**: Enforced at DB level via partial unique index AND validated in service before save.
11. **Period guard**: Every rating write must check `AssessmentPeriodStatus == Open`.

---

## Error / Exception Handling

Reuse the existing exception types in `KnowHub.Domain/Exceptions/`:
- `NotFoundException` — entity not found
- `UnauthorizedException` — caller lacks permission
- `ConflictException` — duplicate/constraint violation (e.g., champion already leads a group)
- `BusinessException` (or `ValidationException`) — business rule violation (e.g., period not open)

The global `ExceptionHandlingMiddleware` already maps these to HTTP status codes — no additional handling needed in controllers.

---

## README Entry

After implementation is complete, add a section to the project README titled **"AI Maturity Assessment Module"** with:
- Purpose (1 paragraph)
- Default rating scale table
- How to assign a Champion and open a period
- API base route
- Frontend route
- Key configuration: generate periods, manage rubrics, assign CoE

See `05-seed-and-acceptance.md` for the README template.
