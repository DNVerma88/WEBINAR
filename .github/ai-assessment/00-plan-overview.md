# AI Usage Rating Module — Plan Overview
## KnowHub · AI Maturity Assessment Module

> **Status**: ✅ Ready for review — designed 2026-03-18  
> **Target agent**: Feature Orchestrator  
> **Reference files**: `01-database-schema.md`, `02-backend-design.md`, `03-frontend-design.md`, `04-implementation-guide.md`, `05-seed-and-acceptance.md`

---

## 1. Purpose

Replace the current Excel-based weekly/bi-weekly AI usage tracking process with a fully governed, auditable, multi-tenant application module built inside KnowHub.

Key outcomes:
- Eliminate Excel dependency
- AI Champions rate employees in configurable periods
- CoE users oversee multiple groups
- Admins manage rubrics, parameters, groups, and periods
- Dashboards and reports at Champion / CoE / Admin / Executive level

---

## 2. Existing Platform Reuse

### Entities Reused As-Is
| Existing Entity | What we reuse |
|----------------|--------------|
| `User` | The single person entity — Champion, CoE, Employee, Admin are all `User` rows. No new user tables. |
| `Tenant` | All AI Assessment data is `TenantId`-scoped. |
| `UserRole` (flags enum) | Platform roles `Admin` / `SuperAdmin` gate module administration. `Manager` gets read-only visibility. |
| `User.Department` | Used for department-level filtering in reports. |
| `User.Designation` | Used as the job-role key for rubric and parameter mappings. |
| `ICurrentUserAccessor` | Reused for all auth checks (`IsAdminOrAbove`, `CurrentUserId`, `TenantId`). |
| `BaseEntity` | All new entities extend `BaseEntity` (`Id`, `TenantId`, `CreatedDate`, `CreatedBy`, `ModifiedOn`, `ModifiedBy`, `RecordVersion`). |

### What Is NOT Duplicated
- No `Employee` table — staff are `User` rows with `UserRole.Employee`.
- No `Manager` table — managers are `User` rows with `UserRole.Manager`.
- No `Champion` or `CoE` global roles — these are **functional assignments** held in `AIAssessmentGroup.ChampionUserId` and `AIAssessmentGroupCoE`.
- No separate `Role` master table — `User.Designation` (string) is used as the job-role key.

### New Dropdown Lookups (populated from existing data)
- **Champion picker** → dropdown from `Users` where `IsActive = true` (same tenant)
- **Employee picker** → dropdown from `Users` where `IsActive = true` (same tenant)
- **CoE picker** → dropdown from `Users` where `IsActive = true` and not already a Champion in another active group

---

## 3. Architecture Fit

| Layer | What is added |
|-------|--------------|
| `KnowHub.Domain` | 11 new entities, 4 new enums |
| `KnowHub.Application` | 6 new service interfaces + DTOs/models |
| `KnowHub.Infrastructure` | EF Core configurations, 6 service implementations |
| `KnowHub.Api` | 7 new controllers under `/api/ai-assessment/...` |
| Frontend | 1 new feature folder `frontend/src/features/ai-assessment/` with tab-based screens |
| Database | 1 new SQL migration file `006_AIAssessmentModule.sql` |

---

## 4. New Entities (Domain Layer)

| # | Entity | Purpose |
|---|--------|---------|
| 1 | `AIAssessmentGroup` | Named group led by one Champion |
| 2 | `AIAssessmentGroupEmployee` | Employee↔Group membership (with history) |
| 3 | `AIAssessmentGroupCoE` | CoE↔Group oversight assignment (with history) |
| 4 | `AssessmentPeriod` | A named rating period (weekly/bi-weekly) with status lifecycle |
| 5 | `RatingScale` | Master: 0 Awareness → 4 Leading |
| 6 | `RubricDefinition` | Role + rating-level rubric with behavior/process/evidence (versioned) |
| 7 | `ParameterMaster` | Configurable scoring parameter (AI Awareness, Prompting, etc.) |
| 8 | `RoleParameterMapping` | Maps parameters to designations with weightage |
| 9 | `EmployeeAssessment` | The transactional rating record per employee per period |
| 10 | `EmployeeAssessmentParameterDetail` | Optional per-parameter sub-ratings (Phase 2) |
| 11 | `AssessmentAuditLog` | Immutable audit trail for all state changes |

---

## 5. New Enums (Domain Layer)

| Enum | Values |
|------|--------|
| `AssessmentPeriodFrequency` | `Weekly = 0`, `BiWeekly = 1` |
| `AssessmentPeriodStatus` | `Draft = 0`, `Open = 1`, `Closed = 2`, `Published = 3` |
| `AssessmentStatus` | `Draft = 0`, `Submitted = 1`, `Reopened = 2` |
| `AssessmentActionType` | `Created`, `Updated`, `Submitted`, `Reopened`, `ChampionChanged`, `CoEAssigned`, `CoERemoved`, `EmployeeAssigned`, `EmployeeRemoved`, `PeriodOpened`, `PeriodClosed`, `PeriodPublished` |

---

## 6. Navigation & UI

### Left Navigation Addition
Add **"AI Assessment"** entry to `AppLayout.tsx` sidebar, visible to all authenticated users (content is role-gated inside the feature).

Route: `/ai-assessment`

### Tab Layout (Role-Aware)

| Tab | Visible To |
|-----|-----------|
| **Dashboard** | All authenticated users |
| **Rate Employees** | Champion (own group) and Admin/SuperAdmin |
| **Groups** | Admin / SuperAdmin only |
| **Periods** | Admin / SuperAdmin only |
| **Rating Scales** | Admin / SuperAdmin only |
| **Rubric** | Admin / SuperAdmin only |
| **Parameters** | Admin / SuperAdmin only |
| **Reports** | Champion (own group), CoE (assigned groups), Admin/SuperAdmin (all) |
| **Audit** | Admin / SuperAdmin only |

---

## 7. Authorization Model

| Action | Who |
|--------|-----|
| Create/edit groups | Admin / SuperAdmin |
| Assign Champion to group | Admin / SuperAdmin |
| Assign CoE to group | Admin / SuperAdmin |
| Assign employees to group | Admin / SuperAdmin |
| Create/open/close periods | Admin / SuperAdmin |
| Manage rating scales | Admin / SuperAdmin |
| Manage rubric definitions | Admin / SuperAdmin |
| Manage parameters | Admin / SuperAdmin |
| Enter / save ratings | Champion (own group only) |
| Submit ratings for period | Champion (own group only) |
| Reopen submitted assessment | Admin / SuperAdmin |
| View own group reports | Champion |
| View assigned group reports | CoE (groups they are assigned to) |
| View all reports | Admin / SuperAdmin |
| View own assessment history | Employee (self) |
| Export reports | Champion, CoE, Admin, SuperAdmin |

**Champion and CoE are NOT new platform roles** — they are functional assignments stored in the group tables. Backend checks: for Champion, query `AIAssessmentGroups.ChampionUserId = currentUserId`. For CoE, query `AIAssessmentGroupCoEs.UserId = currentUserId AND IsActive = true`.

---

## 8. Business Rules

1. One Champion can lead at most **one active group** at a time.
2. One CoE can be assigned to **multiple groups**.
3. Employee group membership changes must preserve history (`EffectiveTo` set on old row, new row inserted).
4. Only `Open` periods allow rating entry; `Draft`, `Closed`, `Published` are read-only.
5. One assessment per `(TenantId, UserId, AssessmentPeriodId)` — unique constraint.
6. Submitted assessments are read-only unless Admin reopens.
7. Rating value must reference an active `RatingScale` record.
8. Rubric is date-valid: `EffectiveFrom <= today <= EffectiveTo OR EffectiveTo IS NULL`.
9. Rubric is versioned — when edited, `EffectiveTo` is set on old version and new row is inserted.
10. Admin can auto-generate 52 weekly periods for a given year.

---

## 9. Implementation Phases

### Phase 1 — Foundation (MVP) ← **Implement first**
**Goal**: Replace Excel, allow Champions to rate employees in open periods.

Deliverables:
- SQL migration `006_AIAssessmentModule.sql`
- All 11 domain entities
- Rating scale seed data (0–4)
- Group management CRUD (admin)
- Assessment period management (admin)
- Rating entry screen for Champions
- Champion dashboard (basic stats)
- Admin dashboard (tenant-wide)
- Left nav entry + tab layout
- Basic validation rules

**Not in Phase 1**: Parameter-wise sub-ratings, full trend reports, export

### Phase 2 — Reports & Dashboards
**Goal**: Give CoE, Admin, and leadership the reports they need.

Deliverables:
- All 12 report types (A through L)
- CoE Dashboard
- Executive Dashboard
- Employee history view
- Export to Excel/CSV

### Phase 3 — Advanced Scoring & Rubric
**Goal**: Enable parameter-wise scoring and rubric-driven guidance.

Deliverables:
- Rubric management UI with versioning
- Parameter master + role parameter mapping
- `EmployeeAssessmentParameterDetail` sub-rating capture
- Weighted scoring calculation
- Rubric display panel in rating entry screen

---

## 10. Risks & Assumptions

| Item | Decision |
|------|----------|
| No Department entity exists | `User.Designation` (string) is used as the job-role key for rubric and parameters. Department remains a plain string for filtering. |
| Platform role vs job role | The `UserRole` flags enum is for platform permissions. `User.Designation` is the job title used in rubric/parameter mapping. |
| Champion uniqueness | Enforced by unique partial index on `AIAssessmentGroups (TenantId, ChampionUserId)` where `IsActive = true`. |
| Period auto-generation | A POST `/api/ai-assessment/periods/generate` endpoint auto-creates 52 (weekly) or 26 (bi-weekly) periods in Draft status for a given year. Admin opens them one by one. |
| Bulk rating | The rating entry screen supports bulk save (save all drafts in one API call) and bulk submit (submit all for period). |
| Excel migration | A one-time data import endpoint `/api/ai-assessment/import/excel` accepts the Excel file and imports historical ratings. Not required for Phase 1. |
| JSONB columns | `ParameterSummaryJson` and `EmployeeAssessmentParameterDetail` are stored as `JSONB` in PostgreSQL / `string` in .NET. |
| Audit of group changes | Group-level changes (champion change, CoE assignment, employee add/remove) are also written to `AssessmentAuditLog` with `RelatedEntityType = "AIAssessmentGroup"`. |

---

## 11. Migration Strategy from Excel

| Step | Action |
|------|--------|
| 1 | Admin creates rating scale (seed data auto-inserts defaults). |
| 2 | Admin creates groups matching Excel "Team Tracking" groups. |
| 3 | Admin assigns Champions and CoE users to groups. |
| 4 | Admin creates or auto-generates assessment periods matching historical weeks. |
| 5 | Admin uses import endpoint to bulk-load historical ratings from Excel. |
| 6 | Champions are trained on the rating entry screen. |
| 7 | Excel process is retired after one parallel period of dual-entry to validate. |

---

## 12. Sample API Payloads

See `02-backend-design.md` for complete list. Key examples:

```json
// POST /api/ai-assessment/groups
{
  "groupName": "Cloud & DevOps Team",
  "groupCode": "CDT-001",
  "description": "Backend infrastructure engineers",
  "championUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}

// POST /api/ai-assessment/assessments/bulk-save
{
  "assessmentPeriodId": "...",
  "groupId": "...",
  "assessments": [
    {
      "userId": "...",
      "ratingScaleId": "...",
      "ratingValue": 2,
      "comment": "Consistently uses AI tools for code review",
      "evidenceNotes": "Demonstrated GitHub Copilot usage in 3 PRs"
    }
  ]
}

// POST /api/ai-assessment/periods/{id}/open
// No body required — transitions Draft → Open

// GET /api/ai-assessment/reports/detailed?periodId=...&groupId=...&rating=2
```
