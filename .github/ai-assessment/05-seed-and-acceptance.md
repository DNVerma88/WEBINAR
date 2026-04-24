# AI Assessment Module — Seed Data, Acceptance Criteria & Task Breakdown

---

## 1. Seed Data SQL

Append this to `database/sql/006_AIAssessmentModule.sql` after the table/index definitions.

```sql
-- ============================================================
-- SEED: Rating Scales (default 5-band scale)
-- Uses the seeded SuperAdmin user (deep.narayan@sierradev.com)
-- and tenant SierraDev as the creator.
-- ============================================================
DO $$
DECLARE
    v_tenant_id UUID;
    v_admin_id  UUID;
BEGIN
    SELECT "Id" INTO v_tenant_id FROM "Tenants" WHERE "Slug" = 'sierradev' LIMIT 1;
    SELECT "Id" INTO v_admin_id  FROM "Users"   WHERE "Email" = 'deep.narayan@sierradev.com' LIMIT 1;

    IF v_tenant_id IS NULL OR v_admin_id IS NULL THEN
        RAISE NOTICE 'Tenant or admin user not found — skipping AI Assessment seed.';
        RETURN;
    END IF;

    INSERT INTO "RatingScales"
        ("Id", "TenantId", "Code", "Name", "NumericValue", "DisplayOrder", "IsActive",
         "CreatedDate", "CreatedBy", "ModifiedOn", "ModifiedBy", "RecordVersion")
    VALUES
        (gen_random_uuid(), v_tenant_id, 'AWARENESS',  'Awareness',  0, 0, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),
        (gen_random_uuid(), v_tenant_id, 'DEVELOPING', 'Developing', 1, 1, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),
        (gen_random_uuid(), v_tenant_id, 'PROFICIENT', 'Proficient', 2, 2, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),
        (gen_random_uuid(), v_tenant_id, 'ADVANCED',   'Advanced',   3, 3, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),
        (gen_random_uuid(), v_tenant_id, 'LEADING',    'Leading',    4, 4, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1)
    ON CONFLICT DO NOTHING;

    -- ── Seed default assessment parameters ──────────────────────────────
    INSERT INTO "ParameterMasters"
        ("Id", "TenantId", "Name", "Code", "Description", "Category", "DisplayOrder", "IsActive",
         "CreatedDate", "CreatedBy", "ModifiedOn", "ModifiedBy", "RecordVersion")
    VALUES
        (gen_random_uuid(), v_tenant_id,
         'AI Awareness & Literacy', 'AI_AWARENESS',
         'Understanding of AI tools, concepts, and applicable use cases in daily work',
         'AIAwareness', 1, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),

        (gen_random_uuid(), v_tenant_id,
         'Prompting Capability', 'PROMPTING',
         'Ability to write clear, structured prompts to get quality AI outputs',
         'Prompting', 2, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),

        (gen_random_uuid(), v_tenant_id,
         'Task Execution with AI', 'TASK_EXECUTION',
         'Applying AI tools to complete role-specific tasks (analysis, coding, writing, etc.)',
         'TaskExecution', 3, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),

        (gen_random_uuid(), v_tenant_id,
         'Review & Validation of AI Output', 'REVIEW_VALIDATION',
         'Critical evaluation of AI-generated content for accuracy and quality',
         'ReviewValidation', 4, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),

        (gen_random_uuid(), v_tenant_id,
         'Workflow Integration', 'WORKFLOW_INTEGRATION',
         'Embedding AI tools into repeatable workflows and delivery processes',
         'WorkflowIntegration', 5, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),

        (gen_random_uuid(), v_tenant_id,
         'Productivity Evidence', 'PRODUCTIVITY_EVIDENCE',
         'Measurable productivity improvements attributed to AI usage',
         'ProductivityEvidence', 6, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),

        (gen_random_uuid(), v_tenant_id,
         'Knowledge Sharing', 'KNOWLEDGE_SHARING',
         'Sharing AI insights, tips, and learnings with the team or wider org',
         'KnowledgeSharing', 7, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),

        (gen_random_uuid(), v_tenant_id,
         'Responsible AI Usage', 'RESPONSIBLE_AI',
         'Adhering to data privacy, ethics, and AI governance guidelines',
         'ResponsibleAI', 8, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1)
    ON CONFLICT DO NOTHING;

END $$;
```

---

## 2. Sample Rubric Definitions

These are **example rubrics** for a "Software Engineer" designation. The admin can add/edit through the UI.
Insert after the rating scale seed, using the same DO $$ block pattern.

```sql
-- Sample rubrics for Software Engineer designation
-- Get RatingScale IDs dynamically for referential integrity
DO $$
DECLARE
    v_tenant_id UUID;
    v_admin_id  UUID;
    v_scale_0   UUID; -- Awareness
    v_scale_1   UUID; -- Developing
    v_scale_2   UUID; -- Proficient
    v_scale_3   UUID; -- Advanced
    v_scale_4   UUID; -- Leading
    v_desig     TEXT := 'Software Engineer';
BEGIN
    SELECT "Id" INTO v_tenant_id FROM "Tenants" WHERE "Slug" = 'sierradev' LIMIT 1;
    SELECT "Id" INTO v_admin_id  FROM "Users"   WHERE "Email" = 'deep.narayan@sierradev.com' LIMIT 1;
    SELECT "Id" INTO v_scale_0   FROM "RatingScales" WHERE "TenantId" = v_tenant_id AND "Code" = 'AWARENESS'  AND "IsActive" = TRUE LIMIT 1;
    SELECT "Id" INTO v_scale_1   FROM "RatingScales" WHERE "TenantId" = v_tenant_id AND "Code" = 'DEVELOPING' AND "IsActive" = TRUE LIMIT 1;
    SELECT "Id" INTO v_scale_2   FROM "RatingScales" WHERE "TenantId" = v_tenant_id AND "Code" = 'PROFICIENT' AND "IsActive" = TRUE LIMIT 1;
    SELECT "Id" INTO v_scale_3   FROM "RatingScales" WHERE "TenantId" = v_tenant_id AND "Code" = 'ADVANCED'   AND "IsActive" = TRUE LIMIT 1;
    SELECT "Id" INTO v_scale_4   FROM "RatingScales" WHERE "TenantId" = v_tenant_id AND "Code" = 'LEADING'    AND "IsActive" = TRUE LIMIT 1;

    IF v_tenant_id IS NULL THEN RETURN; END IF;

    INSERT INTO "RubricDefinitions"
        ("Id","TenantId","DesignationCode","RatingScaleId",
         "BehaviorDescription","ProcessDescription","EvidenceDescription",
         "VersionNo","EffectiveFrom","EffectiveTo","IsActive",
         "CreatedDate","CreatedBy","ModifiedOn","ModifiedBy","RecordVersion")
    VALUES
    (gen_random_uuid(), v_tenant_id, v_desig, v_scale_0,
     'Aware of AI tools like GitHub Copilot and ChatGPT by name. Has not yet applied them regularly.',
     'Attends AI awareness sessions. Explores available tools without structured use.',
     'Can name at least 2 AI tools relevant to their role. Has attended 1 AI awareness session.',
     1, CURRENT_DATE, NULL, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),

    (gen_random_uuid(), v_tenant_id, v_desig, v_scale_1,
     'Experimenting with AI tools for simple tasks like code completion and basic text generation.',
     'Uses AI ad-hoc for isolated tasks. Beginning to build prompt habits.',
     'Has used AI for ≥1 task in last sprint. Can demonstrate a basic prompt-response example.',
     1, CURRENT_DATE, NULL, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),

    (gen_random_uuid(), v_tenant_id, v_desig, v_scale_2,
     'Consistently uses AI for code review, writing unit tests, diagnosing bugs, and documentation.',
     'Integrates AI into regular sprint tasks. Reviews and validates AI output before using.',
     'AI used in ≥3 sprint tasks. At least 1 example of catching an AI hallucination/error.',
     1, CURRENT_DATE, NULL, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),

    (gen_random_uuid(), v_tenant_id, v_desig, v_scale_3,
     'Uses AI for complex refactoring, architecture analysis, automated test generation. Guides peers.',
     'Builds repeatable AI-assisted workflows. Documents prompts and reusable patterns.',
     'Can show productivity metric (time saved, defects reduced). Peer learning sessions delivered.',
     1, CURRENT_DATE, NULL, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1),

    (gen_random_uuid(), v_tenant_id, v_desig, v_scale_4,
     'Champions AI adoption within the team. Defines standards, prompt libraries, and best practices.',
     'Designs AI-integrated workflows for the team/org. Contributes to AI governance.',
     'Team maturity score improved. Published internal guide or conducted org-wide AI workshop.',
     1, CURRENT_DATE, NULL, TRUE, NOW(), v_admin_id, NOW(), v_admin_id, 1)
    ON CONFLICT DO NOTHING;

END $$;
```

---

## 3. Acceptance Criteria

### AC-001 — Group Management
- [ ] Admin can create a new assessment group with a unique GroupCode per tenant.
- [ ] System prevents a Champion from being assigned to more than one active group simultaneously.
- [ ] Admin can change the Champion of a group; audit log records `ChampionChanged`.
- [ ] Admin can add and remove employees from a group; historical records are preserved.
- [ ] Admin can assign and remove CoE users from groups; one CoE can be in multiple groups.
- [ ] Champion sees ONLY their own group(s); CoE sees only assigned groups; Admin sees all.

### AC-002 — Period Management
- [ ] Admin can create periods manually with Name, Frequency, Start/End Dates.
- [ ] Admin can auto-generate 52 weekly or 26 bi-weekly periods for a given year (all as Draft).
- [ ] Period transitions: Draft → Open → Closed → Published (no skipping, no going back).
- [ ] Only one period can be `Open` for the same group at a time (soft rule: alert if opening while another is open).
- [ ] Duplicate period name within the same tenant is rejected.

### AC-003 — Rating Scale Management
- [ ] Default 5-band rating scale (0–4) is seeded automatically from migration.
- [ ] Admin can add custom rating bands or deactivate existing ones.
- [ ] System prevents using a deactivated rating scale in new assessments.

### AC-004 — Rubric Management
- [ ] Admin can define rubrics per Designation + Rating Level.
- [ ] Editing a rubric creates a new version; old version gets EffectiveTo = today.
- [ ] Rating entry screen shows the current date-valid rubric for the employee's designation + selected rating.
- [ ] If no rubric is defined, the UI shows "No rubric defined" — not an error.

### AC-005 — Rating Entry (Champion)
- [ ] Champion can see all active employees in their group on the rating grid.
- [ ] Grid is pre-populated (Draft records created automatically when Champion opens the grid).
- [ ] Champion can select a rating level from dropdown (0–4) for each employee.
- [ ] Champion can add Comment and Evidence Notes per employee.
- [ ] Champion can save all as Draft with one click.
- [ ] Champion can submit all ratings for an Open period with one click.
- [ ] System prevents rating entry when period is not Open.
- [ ] System prevents editing a Submitted record (only Admin can reopen).
- [ ] Previous period rating is shown on the same grid row.
- [ ] Rating change indicator (▲/▼/—) is computed and displayed.

### AC-006 — Assessment Uniqueness
- [ ] System prevents duplicate assessments for the same employee in the same period (DB unique constraint + service-level check).
- [ ] On `BulkSave`, duplicate handling upserts (update existing Draft) rather than failing.

### AC-007 — Audit Trail
- [ ] Every create, update, submit, reopen action generates an `AssessmentAuditLog` record.
- [ ] Group-level changes (Champion change, CoE assignment, employee add/remove) are also logged.
- [ ] Audit log is append-only (no updates or deletes).
- [ ] Admin can view filtered audit log by entity, action, date range.

### AC-008 — Dashboard — Champion
- [ ] Champion sees total employees, rated count, pending count, completion %.
- [ ] Rating distribution chart shows current vs previous period.
- [ ] Data is scoped to Champion's own group only.

### AC-009 — Dashboard — Admin
- [ ] Admin sees tenant-wide completion summary by group.
- [ ] Admin can filter by period.
- [ ] Rating distribution is shown across all groups.

### AC-010 — Reports
- [ ] Detailed report supports all filters: period, group, designation, department, rating level, status.
- [ ] Completion report shows group-level completion % with progress indicator.
- [ ] Employee history shows all past periods for a selected employee with trend line.
- [ ] Export to Excel and CSV works without server error.
- [ ] CoE can only report on groups they are assigned to.
- [ ] Champion can only report on their own group.

### AC-011 — Authorization
- [ ] Champion cannot access or modify other groups.
- [ ] CoE cannot modify ratings — read-only views only.
- [ ] Admin can reopen submitted assessments.
- [ ] Employee can view their own assessment history only.
- [ ] SuperAdmin has all Admin capabilities.

### AC-012 — Multi-tenant Isolation
- [ ] All data queries are filtered by `TenantId`.
- [ ] A user from Tenant A cannot access or read data from Tenant B.
- [ ] Seed data (rating scales) is per-tenant.

---

## 4. Task Breakdown

### Backend Tasks

| # | Task | File(s) | Priority |
|---|------|---------|----------|
| B1 | New enums (`AssessmentEnums.cs`) | Domain/Enums | P1 |
| B2 | Entity: AIAssessmentGroup | Domain/Entities | P1 |
| B3 | Entity: AIAssessmentGroupEmployee | Domain/Entities | P1 |
| B4 | Entity: AIAssessmentGroupCoE | Domain/Entities | P1 |
| B5 | Entity: AssessmentPeriod | Domain/Entities | P1 |
| B6 | Entity: RatingScale | Domain/Entities | P1 |
| B7 | Entity: RubricDefinition | Domain/Entities | P1 |
| B8 | Entity: ParameterMaster | Domain/Entities | P1 |
| B9 | Entity: RoleParameterMapping | Domain/Entities | P1 |
| B10 | Entity: EmployeeAssessment | Domain/Entities | P1 |
| B11 | Entity: EmployeeAssessmentParameterDetail | Domain/Entities | P2 |
| B12 | Entity: AssessmentAuditLog | Domain/Entities | P1 |
| B13 | EF configs for all 11 entities | Infrastructure/Persistence/Configurations | P1 |
| B14 | DbContext update (11 DbSets) | Infrastructure/Persistence | P1 |
| B15 | SQL migration 006 + seed data | database/sql | P1 |
| B16 | DTOs and request models | Application/Models/AIAssessment | P1 |
| B17 | Service interfaces (7) | Application/Contracts | P1 |
| B18 | Validators (6) | Application/Validators/AIAssessment | P1 |
| B19 | AIAssessmentGroupService impl | Infrastructure/Services/AIAssessment | P1 |
| B20 | AssessmentPeriodService impl | Infrastructure/Services/AIAssessment | P1 |
| B21 | RatingScaleService impl | Infrastructure/Services/AIAssessment | P1 |
| B22 | RubricService impl | Infrastructure/Services/AIAssessment | P1 |
| B23 | ParameterService impl | Infrastructure/Services/AIAssessment | P2 |
| B24 | EmployeeAssessmentService impl | Infrastructure/Services/AIAssessment | P1 |
| B25 | AssessmentReportService impl | Infrastructure/Services/AIAssessment | P2 |
| B26 | DI registration | Infrastructure/Extensions | P1 |
| B27 | AIAssessmentGroupsController | Api/Controllers | P1 |
| B28 | AssessmentPeriodsController | Api/Controllers | P1 |
| B29 | RatingScalesController | Api/Controllers | P1 |
| B30 | RubricsController | Api/Controllers | P1 |
| B31 | ParametersController | Api/Controllers | P2 |
| B32 | EmployeeAssessmentsController | Api/Controllers | P1 |
| B33 | AIAssessmentDashboardController | Api/Controllers | P1 |
| B34 | AIAssessmentReportsController | Api/Controllers | P2 |
| B35 | AIAssessmentAuditController | Api/Controllers | P2 |

### Frontend Tasks

| # | Task | File(s) | Priority |
|---|------|---------|----------|
| F1 | Navigation entry in AppLayout | components/AppLayout.tsx | P1 |
| F2 | Route entries | routes.tsx | P1 |
| F3 | Types definitions | features/ai-assessment/types.ts | P1 |
| F4 | API layer | features/ai-assessment/api/aiAssessment.ts | P1 |
| F5 | Role detection hook | hooks/useAIAssessmentRole.ts | P1 |
| F6 | AIAssessmentPage (tab shell) | features/ai-assessment/AIAssessmentPage.tsx | P1 |
| F7 | RatingBadge component | components/RatingBadge.tsx | P1 |
| F8 | DashboardTab (Champion view) | tabs/DashboardTab.tsx | P1 |
| F9 | RateEmployeesTab | tabs/RateEmployeesTab.tsx | P1 |
| F10 | GroupFormDialog | components/GroupFormDialog.tsx | P1 |
| F11 | GroupsTab | tabs/GroupsTab.tsx | P1 |
| F12 | PeriodFormDialog | components/PeriodFormDialog.tsx | P1 |
| F13 | PeriodsTab | tabs/PeriodsTab.tsx | P1 |
| F14 | RatingScaleFormDialog | components/RatingScaleFormDialog.tsx | P1 |
| F15 | RatingScalesTab | tabs/RatingScalesTab.tsx | P1 |
| F16 | RubricFormDialog | components/RubricFormDialog.tsx | P2 |
| F17 | RubricTab | tabs/RubricTab.tsx | P2 |
| F18 | RubricPanel | components/RubricPanel.tsx | P2 |
| F19 | ParameterFormDialog | components/ParameterFormDialog.tsx | P2 |
| F20 | ParametersTab | tabs/ParametersTab.tsx | P2 |
| F21 | ReportsTab | tabs/ReportsTab.tsx | P2 |
| F22 | RatingDistributionChart | components/RatingDistributionChart.tsx | P2 |
| F23 | CompletionProgressCard | components/CompletionProgressCard.tsx | P2 |
| F24 | DashboardTab (Admin + CoE views) | tabs/DashboardTab.tsx | P2 |
| F25 | AIAssessmentGroupDetailPage | AIAssessmentGroupDetailPage.tsx | P2 |
| F26 | EmployeeAssessmentHistoryPage | EmployeeAssessmentHistoryPage.tsx | P2 |
| F27 | AuditTab | tabs/AuditTab.tsx | P2 |

### Database Tasks

| # | Task | File | Priority |
|---|------|------|----------|
| D1 | Write 006_AIAssessmentModule.sql | database/sql | P1 |
| D2 | Append seed data SQL | database/sql/006 | P1 |
| D3 | Apply migration to dev DB | manual run | P1 |

---

## 5. README Module Section Template

Copy this into the project README after the "Phase 3" section:

```markdown
## AI Maturity Assessment Module

Replaces the Excel-based AI usage tracking process with a governed, multi-tenant application module.

**Default Rating Scale:**
| Value | Label |
|-------|-------|
| 0 | Awareness |
| 1 | Developing |
| 2 | Proficient |
| 3 | Advanced |
| 4 | Leading |

**Quick Start:**
1. Run migration `006_AIAssessmentModule.sql` against the database.
2. Log in as Admin → navigate to **AI Assessment → Groups** → create a group and assign a Champion.
3. Navigate to **AI Assessment → Periods** → create or auto-generate periods for the current year.
4. Open a period (Draft → Open).
5. The assigned Champion can now navigate to **AI Assessment → Rate Employees** → select the period and group → rate their team.

**API Base Route:** `/api/ai-assessment/`  
**Frontend Route:** `/ai-assessment`  
**Auth:** All endpoints require a valid JWT bearer token. Champion/CoE access is enforced by group assignment, not platform role.
```

---

## 6. Migration Strategy from Excel

| Step | Action | Owner | Timeline |
|------|--------|-------|---------|
| 1 | Apply `006_AIAssessmentModule.sql` migration | Admin / DevOps | Before go-live |
| 2 | Rating scale auto-seeded, verify in **Rating Scales** tab | Admin | Day 1 |
| 3 | Create Groups matching Excel "Team Tracking" sheet rows | Admin | Day 1–2 |
| 4 | Assign Champions to groups | Admin | Day 1–2 |
| 5 | Assign CoE users to relevant groups | Admin | Day 2 |
| 6 | Import employee rosters into each group | Admin | Day 2–3 |
| 7 | Generate weekly periods for current year | Admin | Day 3 |
| 8 | Champion training session on rating entry screen | Champion | Day 4–5 |
| 9 | Run parallel: Excel + App for 1 period to validate data fidelity | Champion + Admin | Week 1 |
| 10 | Import historical ratings via import endpoint (optional) | Admin | Week 1–2 |
| 11 | Retire Excel tracker, fully migrate to application | All | Week 2+ |

---

## 7. Key Report Calculation Logic

### Average Maturity Score
```
AvgMaturityScore = SUM(RatingValue) / COUNT(Assessments)
```
Displayed as a decimal to 2 places (e.g., 2.35 / 4.00).

### Trend: Period-over-Period Change
```
Change = AvgMaturityScore(currentPeriod) - AvgMaturityScore(previousPeriod)
Improving: Change > 0
Declining: Change < 0
Stable:    Change = 0
```

### Completion Percentage
```
CompletionPct = (SubmittedCount / TotalExpectedCount) * 100
TotalExpected = Active employee count in group at period start date
```

### Rating Band Distribution
For each rating level (0–4), count how many employees achieved that band:
```sql
SELECT "RatingValue", COUNT(*) as "Count"
FROM "EmployeeAssessments"
WHERE "AssessmentPeriodId" = @periodId AND "TenantId" = @tenantId
  AND "Status" = 1  -- Submitted only
GROUP BY "RatingValue"
ORDER BY "RatingValue"
```

### Weighted Score (Phase 2)
```
WeightedScore = SUM(ParameterRatingValue * Weightage / 100)
```
Only calculated when `EmployeeAssessmentParameterDetails` exist for the assessment.
