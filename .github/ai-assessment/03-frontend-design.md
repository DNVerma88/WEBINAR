# AI Assessment Module — Frontend Design
## Routes · Screens · Components · API Layer · Navigation

> Reference this file when building the React frontend for the AI Assessment module.  
> Stack: React 18 + MUI v5 + React Router v6 + Axios + React Query + React Hook Form + TypeScript

---

## 1. Navigation — AppLayout.tsx Update

**File**: `frontend/src/components/AppLayout.tsx`

Add the following entry to the nav items list **after "Analytics"** (or at the end of the list):

```tsx
{ label: 'AI Assessment', path: '/ai-assessment', icon: <PsychologyIcon /> }
```

Use MUI icon `PsychologyIcon` from `@mui/icons-material/Psychology` (AI brain icon).  
Visible to all authenticated users. Role-specific content is handled within the feature.

---

## 2. Routes Update

**File**: `frontend/src/routes.tsx`

Add these routes inside the `<PrivateRoute>` / `<AppLayout>` wrapper:

```tsx
// AI Assessment Module
{ path: '/ai-assessment', element: <AIAssessmentPage /> }
{ path: '/ai-assessment/groups/:id', element: <AIAssessmentGroupDetailPage /> }
{ path: '/ai-assessment/employees/:userId/history', element: <EmployeeAssessmentHistoryPage /> }
```

---

## 3. Feature Folder Structure

```
frontend/src/features/ai-assessment/
├── AIAssessmentPage.tsx                  ← Main page with tab layout (role-aware)
├── AIAssessmentGroupDetailPage.tsx       ← Group detail with employee list
├── EmployeeAssessmentHistoryPage.tsx     ← Employee's rating history
├── tabs/
│   ├── DashboardTab.tsx                  ← Role-aware dashboard (champion/coe/admin/exec)
│   ├── RateEmployeesTab.tsx              ← Rating entry grid (Champion / Admin)
│   ├── GroupsTab.tsx                     ← Group CRUD (Admin only)
│   ├── PeriodsTab.tsx                    ← Period management (Admin only)
│   ├── RatingScalesTab.tsx               ← Rating scale master (Admin only)
│   ├── RubricTab.tsx                     ← Rubric management (Admin only)
│   ├── ParametersTab.tsx                 ← Parameter master + role mapping (Admin only)
│   ├── ReportsTab.tsx                    ← All reports (scoped by role)
│   └── AuditTab.tsx                      ← Audit log viewer (Admin only)
├── components/
│   ├── GroupFormDialog.tsx               ← Create/Edit group dialog
│   ├── PeriodFormDialog.tsx              ← Create/Edit period dialog
│   ├── RatingScaleFormDialog.tsx         ← Create/Edit rating scale dialog
│   ├── RubricFormDialog.tsx              ← Create/Edit rubric definition dialog
│   ├── ParameterFormDialog.tsx           ← Create/Edit parameter dialog
│   ├── RoleMappingFormDialog.tsx         ← Create/Edit role-parameter mapping dialog
│   ├── AssessmentRatingRow.tsx           ← Single row in the rating grid
│   ├── RubricPanel.tsx                   ← Shows rubric for employee's designation + rating
│   ├── RatingBadge.tsx                   ← Colored chip: 0=grey, 1=orange, 2=blue, 3=green, 4=purple
│   ├── EmployeeHistoryChart.tsx          ← Line/bar chart: employee's rating over periods
│   ├── RatingDistributionChart.tsx       ← Horizontal bar chart: band distribution
│   ├── CompletionProgressCard.tsx        ← Progress ring card
│   └── GroupMemberDialog.tsx             ← Add/remove employee or CoE from group
└── api/
    └── aiAssessment.ts                   ← All API calls for this module
```

---

## 4. API Layer

**File**: `frontend/src/features/ai-assessment/api/aiAssessment.ts`

```typescript
import api from '../../../shared/api/axios'; // reuse existing Axios instance

const BASE = '/api/ai-assessment';

// ── Groups ──────────────────────────────────────────────────────────
export const aiAssessmentGroupsApi = {
  list:             (params?: any)         => api.get(`${BASE}/groups`, { params }),
  get:              (id: string)           => api.get(`${BASE}/groups/${id}`),
  create:           (body: any)            => api.post(`${BASE}/groups`, body),
  update:           (id: string, body: any)=> api.put(`${BASE}/groups/${id}`, body),
  deactivate:       (id: string)           => api.delete(`${BASE}/groups/${id}`),
  listEmployees:    (groupId: string)      => api.get(`${BASE}/groups/${groupId}/employees`),
  addEmployee:      (groupId: string, b: any) => api.post(`${BASE}/groups/${groupId}/employees`, b),
  removeEmployee:   (groupId: string, userId: string) => api.delete(`${BASE}/groups/${groupId}/employees/${userId}`),
  listCoEs:         (groupId: string)      => api.get(`${BASE}/groups/${groupId}/coe`),
  assignCoE:        (groupId: string, b: any) => api.post(`${BASE}/groups/${groupId}/coe`, b),
  removeCoE:        (groupId: string, userId: string) => api.delete(`${BASE}/groups/${groupId}/coe/${userId}`),
};

// ── Periods ──────────────────────────────────────────────────────────
export const assessmentPeriodsApi = {
  list:     (params?: any)         => api.get(`${BASE}/periods`, { params }),
  get:      (id: string)           => api.get(`${BASE}/periods/${id}`),
  create:   (body: any)            => api.post(`${BASE}/periods`, body),
  update:   (id: string, body: any)=> api.put(`${BASE}/periods/${id}`, body),
  open:     (id: string)           => api.post(`${BASE}/periods/${id}/open`),
  close:    (id: string)           => api.post(`${BASE}/periods/${id}/close`),
  publish:  (id: string)           => api.post(`${BASE}/periods/${id}/publish`),
  generate: (body: any)            => api.post(`${BASE}/periods/generate`, body),
};

// ── Rating Scales ─────────────────────────────────────────────────────
export const ratingScalesApi = {
  list:       ()                   => api.get(`${BASE}/rating-scales`),
  create:     (body: any)          => api.post(`${BASE}/rating-scales`, body),
  update:     (id: string, b: any) => api.put(`${BASE}/rating-scales/${id}`, b),
  deactivate: (id: string)         => api.delete(`${BASE}/rating-scales/${id}`),
};

// ── Rubrics ───────────────────────────────────────────────────────────
export const rubricsApi = {
  list:          (params?: any)         => api.get(`${BASE}/rubrics`, { params }),
  byDesignation: (code: string)         => api.get(`${BASE}/rubrics/by-designation`, { params: { designationCode: code } }),
  create:        (body: any)            => api.post(`${BASE}/rubrics`, body),
  update:        (id: string, b: any)   => api.put(`${BASE}/rubrics/${id}`, b),
};

// ── Parameters ────────────────────────────────────────────────────────
export const parametersApi = {
  list:             ()                   => api.get(`${BASE}/parameters`),
  create:           (body: any)          => api.post(`${BASE}/parameters`, body),
  update:           (id: string, b: any) => api.put(`${BASE}/parameters/${id}`, b),
  listRoleMappings: (params?: any)       => api.get(`${BASE}/parameters/role-mappings`, { params }),
  upsertMapping:    (body: any)          => api.post(`${BASE}/parameters/role-mappings`, body),
  removeMapping:    (id: string)         => api.delete(`${BASE}/parameters/role-mappings/${id}`),
};

// ── Assessments ───────────────────────────────────────────────────────
export const assessmentsApi = {
  list:             (params?: any)         => api.get(`${BASE}/assessments`, { params }),
  get:              (id: string)           => api.get(`${BASE}/assessments/${id}`),
  getGrid:          (params: any)          => api.get(`${BASE}/assessments/grid`, { params }),
  saveDraft:        (body: any)            => api.post(`${BASE}/assessments/draft`, body),
  bulkSave:         (body: any)            => api.post(`${BASE}/assessments/bulk-save`, body),
  submit:           (id: string)           => api.post(`${BASE}/assessments/${id}/submit`),
  bulkSubmit:       (body: any)            => api.post(`${BASE}/assessments/bulk-submit`, body),
  reopen:           (id: string, r: any)   => api.post(`${BASE}/assessments/${id}/reopen`, r),
};

// ── Dashboard ─────────────────────────────────────────────────────────
export const assessmentDashboardApi = {
  champion:  (params?: any) => api.get(`${BASE}/dashboard/champion`, { params }),
  coe:       (params?: any) => api.get(`${BASE}/dashboard/coe`, { params }),
  admin:     (params?: any) => api.get(`${BASE}/dashboard/admin`, { params }),
  executive: (params?: any) => api.get(`${BASE}/dashboard/executive`, { params }),
};

// ── Reports ───────────────────────────────────────────────────────────
export const assessmentReportsApi = {
  detailed:          (params?: any) => api.get(`${BASE}/reports/detailed`, { params }),
  completion:        (params?: any) => api.get(`${BASE}/reports/completion`, { params }),
  groupDistribution: (params?: any) => api.get(`${BASE}/reports/group-distribution`, { params }),
  roleDistribution:  (params?: any) => api.get(`${BASE}/reports/role-distribution`, { params }),
  employeeHistory:   (userId: string) => api.get(`${BASE}/reports/employee-history`, { params: { userId } }),
  trend:             (params?: any) => api.get(`${BASE}/reports/trend`, { params }),
  risk:              (params?: any) => api.get(`${BASE}/reports/risk`, { params }),
  improvement:       (params?: any) => api.get(`${BASE}/reports/improvement`, { params }),
  exportExcel:       (params?: any) => api.get(`${BASE}/reports/export/excel`, { params, responseType: 'blob' }),
  exportCsv:         (params?: any) => api.get(`${BASE}/reports/export/csv`, { params, responseType: 'blob' }),
};

// ── Audit ─────────────────────────────────────────────────────────────
export const assessmentAuditApi = {
  list: (params?: any) => api.get(`${BASE}/audit`, { params }),
};
```

---

## 5. Main Page — AIAssessmentPage.tsx

This is the entry point. It renders role-aware tabs.

```tsx
// frontend/src/features/ai-assessment/AIAssessmentPage.tsx
// Tab visibility rules:
//   isAdminOrAbove → all tabs
//   isChampion (has active group) → Dashboard, Rate Employees, Reports
//   isCoE (has assigned groups) → Dashboard, Reports
//   others → Dashboard only (shows own assessment history)

const TABS = [
  { label: 'Dashboard',      value: 'dashboard',      roles: 'all' },
  { label: 'Rate Employees', value: 'rate',            roles: 'champion|admin' },
  { label: 'Groups',         value: 'groups',          roles: 'admin' },
  { label: 'Periods',        value: 'periods',         roles: 'admin' },
  { label: 'Rating Scales',  value: 'scales',          roles: 'admin' },
  { label: 'Rubric',         value: 'rubric',          roles: 'admin' },
  { label: 'Parameters',     value: 'parameters',      roles: 'admin' },
  { label: 'Reports',        value: 'reports',         roles: 'champion|coe|admin' },
  { label: 'Audit',          value: 'audit',           roles: 'admin' },
];
```

Pattern: MUI `<Tabs>` + `<Tab>` with `value` mapped to tab content, identical to `AnalyticsDashboardPage.tsx`. Use MUI `<Box>` with `role="tabpanel"` for each panel.

---

## 6. Dashboard Tab — DashboardTab.tsx

Role-aware rendering:

### If isChampion
- **Stat row**: Total Employees | Rated | Pending | Completion %
- **Period selector** (dropdown filtered to current year Open/Closed periods)
- **Group selector** (Champion sees own group only)
- **Rating distribution bar** (current period vs previous period)
- **Employee status table** (Name | Designation | CurrentRating | PreviousRating | Change | Status)

### If isCoE
- **Groups table** (GroupName | Champion | Total | Rated | Pending | Completion%)
- **Period selector**
- **Rating distribution by group** (horizontal stacked bar)

### If isAdminOrAbove
- **Tenant-wide stats**: Total groups | Total employees | Avg completion% | Current period status
- **Group completion table** (Group | Champion | Total | Submitted | Pending | Completion%)
- **Period selector**
- **Rating distribution across all groups** (stacked bar by group)

### Executive view (ManagerOrAbove)
- **Org maturity score** (avg rating value, displayed as a gauge/number)
- **Rating band pie chart**
- **Top 5 improving groups**
- **Bottom 5 groups by maturity**
- **Historical trend line** (avg rating by period)

---

## 7. Rate Employees Tab — RateEmployeesTab.tsx

This is the core transactional screen for Champions (and Admins).

### Filters row:
- **Period** (dropdown: AssessmentPeriods, default = latest Open) — required
- **Group** (dropdown: Champion sees own group; Admin sees all) — required
- **Employee name** (text search)
- **Designation** (text filter)
- **Rating** (dropdown: all rating levels + "Not Rated")
- **Status** (All / Draft / Submitted)

### Action bar:
- **[Save All Drafts]** button → calls `bulkSave`
- **[Submit All]** button → calls `bulkSubmit` (only enabled if period is Open)
- Period status badge (Open → green, Closed → red, Published → blue)
- Rubric toggle button (shows/hides rubric panel)

### Rating Grid (MUI DataGrid or manual table):
Columns:
1. Employee Name (link to profile)
2. Designation
3. Department
4. Previous Rating (read-only RatingBadge chip)
5. Current Rating (dropdown → RatingScale options; disabled if period not Open or status Submitted)
6. Change (▲ / ▼ / — based on ratingChange value, colored)
7. Comment (text field)
8. Evidence Notes (text field, collapsible)
9. Status (Draft/Submitted badge)
10. (Action) Reopen button (Admin only, for Submitted rows)

### Rubric Side Panel (collapsible right panel or dialog):
- Shows for selected row's designation + current rating
- Displays: **Behavior**, **Process**, **Evidence** from matching `RubricDefinition`
- If no rubric found, show "No rubric defined for this designation and rating level."

### Rules:
- If period status ≠ Open, entire grid is read-only
- If row status = Submitted, that row is read-only (unless reopened by Admin)
- Show save indicator per row (dirty / saved / error)

---

## 8. Groups Tab — GroupsTab.tsx (Admin only)

### Content:
- MUI DataGrid with columns: Group Code | Group Name | Champion Name | Active Employees | Status | Actions (Edit / Manage Members / Deactivate)
- **[+ New Group]** button → opens `GroupFormDialog`
- Search bar (group name / group code / champion name)
- Status filter (Active / Inactive / All)

### GroupFormDialog:
Fields:
- Group Name (text, required)
- Group Code (text, required, unique)
- Description (textarea)
- Champion (Autocomplete → Users list, filtered by `isActive = true`, shows "Name (Designation)" label)

### Group Detail (click "Manage Members"):
- Navigate to `/ai-assessment/groups/:id`
- Shows 2 sub-tabs: **Employees** | **CoE**
- Each sub-tab: list of current active members + Add/Remove buttons
- Add dialog: Autocomplete search from Users table
- History toggle: shows full membership history (EffectiveFrom/EffectiveTo)

---

## 9. Periods Tab — PeriodsTab.tsx (Admin only)

### Content:
- MUI DataGrid: Period Name | Frequency | Start Date | End Date | Year | Week# | Status | Actions
- Filter by Year (default: current year), Status, Frequency
- **[+ New Period]** button → opens `PeriodFormDialog`
- **[Generate Periods]** button → dialog to select Year + Frequency, calls generate endpoint
- Row actions: Edit (Draft only) | Open | Close | Publish (status-dependent)

### PeriodFormDialog:
Fields:
- Name (text, auto-suggested: "2026-W{n}")
- Frequency (select: Weekly / Bi-Weekly)
- Start Date (date picker)
- End Date (date picker)
- Year (number, auto-from Start Date)
- Week Number (optional number)

---

## 10. Rating Scales Tab — RatingScalesTab.tsx (Admin only)

### Content:
- Table: Numeric Value | Code | Name | Display Order | Active | Actions (Edit / Deactivate)
- Sorted by DisplayOrder
- **[+ New Scale]** button → `RatingScaleFormDialog`

### Default seeded values (shown as read-only reference):
| Value | Code | Name |
|-------|------|------|
| 0 | AWARENESS | Awareness |
| 1 | DEVELOPING | Developing |
| 2 | PROFICIENT | Proficient |
| 3 | ADVANCED | Advanced |
| 4 | LEADING | Leading |

---

## 11. Rubric Tab — RubricTab.tsx (Admin only)

### Content:
- Filters: Designation (text autocomplete), Rating Level (dropdown)
- Table: Designation | Rating Level | Version | Effective From | Effective To | Active | Actions (Edit / View)
- **[+ New Rubric]** button → `RubricFormDialog`

### RubricFormDialog:
Fields:
- Designation Code (text, or autocomplete from distinct User.Designation values)
- Rating Scale (dropdown from RatingScales)
- Behavior Description (rich textarea)
- Process Description (rich textarea)
- Evidence Description (rich textarea)
- Effective From (date)
- Effective To (date, optional)

On Edit: creates new version (old EffectiveTo = today - 1 day), new VersionNo++.

---

## 12. Parameters Tab — ParametersTab.tsx (Admin only)

Two sub-tabs: **Parameters** | **Role Mappings**

### Parameters Sub-tab:
- Table: Code | Name | Category | Display Order | Active | Actions
- **[+ New Parameter]** button

### Role Mappings Sub-tab:
- Filter by Designation
- Table: Designation | Parameter | Weightage | Mandatory | Display Order | Active | Actions
- **[+ Add Mapping]** button

---

## 13. Reports Tab — ReportsTab.tsx

Uses MUI Tabs within tabs (nested tabs for each report type).

### Report sub-tabs (role-aware):

| Sub-tab | Visible to |
|---------|-----------|
| Detailed Report | Champion, CoE, Admin |
| Completion Status | Champion, CoE, Admin |
| Group Distribution | CoE, Admin |
| Role Distribution | Admin |
| Employee History | Admin + self |
| Trend | Admin |
| Risk / Low Adoption | Admin |
| Improvement | Admin |
| Export | Champion, CoE, Admin |

### Common filter bar across all reports:
- Period selector (dropdown)
- Group selector (Champion: own group only; CoE: assigned groups; Admin: all)
- [Refresh] button

### Detailed Report sub-tab:
Additional filters: Role/Designation | Department | Rating Level | Status | Employee name search  
Table columns: Employee | Designation | Dept | Group | Champion | CoE | Period | Prev Rating | Curr Rating | Change | Comment | Status  
Pagination: server-side

### Completion Status sub-tab:
Summary cards: Total Expected | Completed | Pending | Completion %  
Table: Group | Champion | Total | Completed | Pending | Completion % (with LinearProgress bar)

### Group Distribution sub-tab:
For each group: horizontal stacked bar → count per rating band  
Color coding: 0=grey, 1=orange, 2=blue, 3=green, 4=purple

### Employee History sub-tab:
Employee autocomplete search → line chart (period on x-axis, rating value on y-axis)  
Below chart: table with Period | Rating | Change | Comment | Status

### Trend sub-tab:
Line chart: average rating over periods  
Stacked area chart: distribution over time

### Export sub-tab:
- Select report type (Detailed / Completion / Distribution / History)
- Apply filters
- [Export Excel] → downloads `.xlsx`
- [Export CSV] → downloads `.csv`

---

## 14. Audit Tab — AuditTab.tsx (Admin only)

Filters: Action Type | Changed By | Date Range | Entity ID  
Table: Timestamp | Entity Type | Entity ID | Action | Changed By | Old Value (expandable JSON) | New Value (expandable JSON) | Remarks

---

## 15. Shared Type Definitions

**File**: `frontend/src/features/ai-assessment/types.ts`

```typescript
export interface AIAssessmentGroup {
  id: string;
  groupName: string;
  groupCode: string;
  description?: string;
  championUserId: string;
  championName: string;
  activeEmployeeCount: number;
  isActive: boolean;
  createdDate: string;
}

export type AssessmentPeriodStatus = 'Draft' | 'Open' | 'Closed' | 'Published';
export type AssessmentStatus = 'Draft' | 'Submitted' | 'Reopened';
export type AssessmentPeriodFrequency = 'Weekly' | 'BiWeekly';

export interface AssessmentPeriod {
  id: string;
  name: string;
  frequency: AssessmentPeriodFrequency;
  startDate: string;
  endDate: string;
  year: number;
  weekNumber?: number;
  status: AssessmentPeriodStatus;
  isActive: boolean;
}

export interface RatingScale {
  id: string;
  code: string;
  name: string;
  numericValue: number;
  displayOrder: number;
  isActive: boolean;
}

export interface EmployeeAssessment {
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
}

export interface ChampionDashboard {
  totalEmployees: number;
  rated: number;
  pending: number;
  completionPercent: number;
  currentRatingDistribution: RatingBandCount[];
  previousRatingDistribution: RatingBandCount[];
  periodName: string;
  groupName: string;
}

export interface RatingBandCount {
  ratingName: string;
  numericValue: number;
  count: number;
}
```

---

## 16. RatingBadge Component

**File**: `frontend/src/features/ai-assessment/components/RatingBadge.tsx`

```tsx
// Color map for rating levels
const RATING_COLORS: Record<number, { bg: string; color: string }> = {
  0: { bg: '#9e9e9e', color: '#fff' },  // Awareness — grey
  1: { bg: '#ff9800', color: '#fff' },  // Developing — orange
  2: { bg: '#2196f3', color: '#fff' },  // Proficient — blue
  3: { bg: '#4caf50', color: '#fff' },  // Advanced — green
  4: { bg: '#9c27b0', color: '#fff' },  // Leading — purple
};

export function RatingBadge({ value, label }: { value: number; label: string }) {
  const colors = RATING_COLORS[value] ?? { bg: '#eee', color: '#333' };
  return (
    <Chip
      label={`${value} – ${label}`}
      size="small"
      sx={{ backgroundColor: colors.bg, color: colors.color, fontWeight: 600 }}
    />
  );
}
```

---

## 17. React Query Keys

Suggested query key pattern for cache management:

```typescript
export const ASSESSMENT_KEYS = {
  groups:          (params?: any) => ['ai-assessment', 'groups', params],
  group:           (id: string)   => ['ai-assessment', 'groups', id],
  periods:         (params?: any) => ['ai-assessment', 'periods', params],
  ratingScales:    ()             => ['ai-assessment', 'rating-scales'],
  rubrics:         (params?: any) => ['ai-assessment', 'rubrics', params],
  parameters:      ()             => ['ai-assessment', 'parameters'],
  assessmentGrid:  (groupId: string, periodId: string) =>
                                     ['ai-assessment', 'grid', groupId, periodId],
  dashboard:       (role: string, params?: any) =>
                                     ['ai-assessment', 'dashboard', role, params],
  reports:         (type: string, params?: any) =>
                                     ['ai-assessment', 'reports', type, params],
  audit:           (params?: any) => ['ai-assessment', 'audit', params],
};
```

---

## 18. Role Detection Helper

**File**: `frontend/src/features/ai-assessment/hooks/useAIAssessmentRole.ts`

```typescript
import { useAuth } from '../../../shared/hooks/useAuth'; // existing hook
import { useQuery } from '@tanstack/react-query';
import { aiAssessmentGroupsApi } from '../api/aiAssessment';

export function useAIAssessmentRole() {
  const { user } = useAuth();

  const isAdminOrAbove = user?.isAdmin || user?.isSuperAdmin;

  // Fetch groups where user is Champion
  const { data: myChampionGroups } = useQuery({
    queryKey: ['ai-assessment', 'my-champion-groups'],
    queryFn: () => aiAssessmentGroupsApi.list({ championId: user?.id, isActive: true }),
    enabled: !!user && !isAdminOrAbove,
    select: (r) => r.data.items ?? [],
  });

  // Fetch groups where user is CoE
  const { data: myCoEGroups } = useQuery({
    queryKey: ['ai-assessment', 'my-coe-groups'],
    queryFn: () => aiAssessmentGroupsApi.list({ coeId: user?.id, isActive: true }),
    enabled: !!user && !isAdminOrAbove,
    select: (r) => r.data.items ?? [],
  });

  const isChampion = (myChampionGroups?.length ?? 0) > 0;
  const isCoE      = (myCoEGroups?.length ?? 0) > 0;

  return {
    isAdminOrAbove,
    isChampion,
    isCoE,
    myChampionGroups: myChampionGroups ?? [],
    myCoEGroups:      myCoEGroups ?? [],
  };
}
```
