-- ============================================================
-- KnowHub Database Schema
-- Migration: 006_AIAssessmentModule.sql
-- AI Usage Rating / AI Maturity Assessment Module
-- PascalCase columns, UUID PKs, TIMESTAMPTZ, tenant-isolated
-- ============================================================

-- ============================================================
-- AI ASSESSMENT GROUPS
-- Groups led by one Champion. One Champion → one active group max.
-- ============================================================
CREATE TABLE "AIAssessmentGroups" (
    "Id"              UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"        UUID            NOT NULL,
    "GroupName"       VARCHAR(200)    NOT NULL,
    "GroupCode"       VARCHAR(50)     NOT NULL,
    "Description"     TEXT,
    "ChampionUserId"  UUID            NOT NULL,
    "IsActive"        BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"       UUID            NOT NULL,
    "ModifiedOn"      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"      UUID            NOT NULL,
    "RecordVersion"   INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_AIAssessmentGroups" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AIAssessmentGroups_Tenants_TenantId"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AIAssessmentGroups_Users_ChampionUserId"
        FOREIGN KEY ("ChampionUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_AIAssessmentGroups_TenantId_GroupCode"
    ON "AIAssessmentGroups" ("TenantId", "GroupCode");

CREATE UNIQUE INDEX "IX_AIAssessmentGroups_TenantId_ChampionUserId_Active"
    ON "AIAssessmentGroups" ("TenantId", "ChampionUserId")
    WHERE "IsActive" = TRUE;

CREATE INDEX "IX_AIAssessmentGroups_TenantId"
    ON "AIAssessmentGroups" ("TenantId");

CREATE INDEX "IX_AIAssessmentGroups_TenantId_IsActive"
    ON "AIAssessmentGroups" ("TenantId", "IsActive");

-- ============================================================
-- AI ASSESSMENT GROUP EMPLOYEES
-- Historical membership. EffectiveTo NULL = currently active.
-- ============================================================
CREATE TABLE "AIAssessmentGroupEmployees" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "GroupId"       UUID            NOT NULL,
    "UserId"        UUID            NOT NULL,
    "EffectiveFrom" TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "EffectiveTo"   TIMESTAMPTZ,
    "IsActive"      BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_AIAssessmentGroupEmployees" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AIAssessmentGroupEmployees_Tenants_TenantId"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AIAssessmentGroupEmployees_AIAssessmentGroups_GroupId"
        FOREIGN KEY ("GroupId") REFERENCES "AIAssessmentGroups" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AIAssessmentGroupEmployees_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_AIAssessmentGroupEmployees_TenantId_UserId_Active"
    ON "AIAssessmentGroupEmployees" ("TenantId", "UserId")
    WHERE "IsActive" = TRUE;

CREATE INDEX "IX_AIAssessmentGroupEmployees_TenantId_GroupId"
    ON "AIAssessmentGroupEmployees" ("TenantId", "GroupId");

CREATE INDEX "IX_AIAssessmentGroupEmployees_TenantId_UserId"
    ON "AIAssessmentGroupEmployees" ("TenantId", "UserId");

-- ============================================================
-- AI ASSESSMENT GROUP CoE
-- CoE oversight assignments. One CoE can oversee many groups.
-- ============================================================
CREATE TABLE "AIAssessmentGroupCoEs" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "GroupId"       UUID            NOT NULL,
    "UserId"        UUID            NOT NULL,
    "EffectiveFrom" TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "EffectiveTo"   TIMESTAMPTZ,
    "IsActive"      BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_AIAssessmentGroupCoEs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AIAssessmentGroupCoEs_Tenants_TenantId"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AIAssessmentGroupCoEs_AIAssessmentGroups_GroupId"
        FOREIGN KEY ("GroupId") REFERENCES "AIAssessmentGroups" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AIAssessmentGroupCoEs_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_AIAssessmentGroupCoEs_TenantId_GroupId_UserId_Active"
    ON "AIAssessmentGroupCoEs" ("TenantId", "GroupId", "UserId")
    WHERE "IsActive" = TRUE;

CREATE INDEX "IX_AIAssessmentGroupCoEs_TenantId_GroupId"
    ON "AIAssessmentGroupCoEs" ("TenantId", "GroupId");

CREATE INDEX "IX_AIAssessmentGroupCoEs_TenantId_UserId"
    ON "AIAssessmentGroupCoEs" ("TenantId", "UserId");

-- ============================================================
-- ASSESSMENT PERIODS
-- Configurable named periods. Frequency: 0=Weekly, 1=BiWeekly.
-- Status: 0=Draft, 1=Open, 2=Closed, 3=Published
-- ============================================================
CREATE TABLE "AssessmentPeriods" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "Name"          VARCHAR(200)    NOT NULL,
    "Frequency"     INT             NOT NULL DEFAULT 0,
    "StartDate"     DATE            NOT NULL,
    "EndDate"       DATE            NOT NULL,
    "Year"          INT             NOT NULL,
    "WeekNumber"    INT,
    "Status"        INT             NOT NULL DEFAULT 0,
    "IsActive"      BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_AssessmentPeriods" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AssessmentPeriods_Tenants_TenantId"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_AssessmentPeriods_TenantId_Name"
    ON "AssessmentPeriods" ("TenantId", "Name");

CREATE INDEX "IX_AssessmentPeriods_TenantId_Year_Status"
    ON "AssessmentPeriods" ("TenantId", "Year", "Status");

CREATE INDEX "IX_AssessmentPeriods_TenantId"
    ON "AssessmentPeriods" ("TenantId");

-- ============================================================
-- RATING SCALES
-- Master lookup: 0=Awareness, 1=Developing, 2=Proficient,
--                3=Advanced, 4=Leading
-- ============================================================
CREATE TABLE "RatingScales" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "Code"          VARCHAR(50)     NOT NULL,
    "Name"          VARCHAR(100)    NOT NULL,
    "NumericValue"  INT             NOT NULL,
    "DisplayOrder"  INT             NOT NULL DEFAULT 0,
    "IsActive"      BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_RatingScales" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RatingScales_Tenants_TenantId"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_RatingScales_TenantId_Code"
    ON "RatingScales" ("TenantId", "Code");

CREATE UNIQUE INDEX "IX_RatingScales_TenantId_NumericValue_Active"
    ON "RatingScales" ("TenantId", "NumericValue")
    WHERE "IsActive" = TRUE;

CREATE INDEX "IX_RatingScales_TenantId"
    ON "RatingScales" ("TenantId");

-- ============================================================
-- RUBRIC DEFINITIONS
-- Role (Designation) + RatingScale → Behavior/Process/Evidence
-- Versioned: old record gets EffectiveTo set; new record inserted.
-- ============================================================
CREATE TABLE "RubricDefinitions" (
    "Id"                    UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"              UUID            NOT NULL,
    "DesignationCode"       VARCHAR(150)    NOT NULL,
    "RatingScaleId"         UUID            NOT NULL,
    "BehaviorDescription"   TEXT            NOT NULL,
    "ProcessDescription"    TEXT            NOT NULL,
    "EvidenceDescription"   TEXT            NOT NULL,
    "VersionNo"             INT             NOT NULL DEFAULT 1,
    "EffectiveFrom"         DATE            NOT NULL,
    "EffectiveTo"           DATE,
    "IsActive"              BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"             UUID            NOT NULL,
    "ModifiedOn"            TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"            UUID            NOT NULL,
    "RecordVersion"         INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_RubricDefinitions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RubricDefinitions_Tenants_TenantId"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_RubricDefinitions_RatingScales_RatingScaleId"
        FOREIGN KEY ("RatingScaleId") REFERENCES "RatingScales" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_RubricDefinitions_TenantId_DesignationCode_RatingScaleId"
    ON "RubricDefinitions" ("TenantId", "DesignationCode", "RatingScaleId");

CREATE INDEX "IX_RubricDefinitions_TenantId_IsActive"
    ON "RubricDefinitions" ("TenantId", "IsActive");

CREATE INDEX "IX_RubricDefinitions_TenantId"
    ON "RubricDefinitions" ("TenantId");

-- ============================================================
-- PARAMETER MASTERS
-- ============================================================
CREATE TABLE "ParameterMasters" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "Name"          VARCHAR(200)    NOT NULL,
    "Code"          VARCHAR(50)     NOT NULL,
    "Description"   TEXT,
    "Category"      VARCHAR(100)    NOT NULL,
    "DisplayOrder"  INT             NOT NULL DEFAULT 0,
    "IsActive"      BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_ParameterMasters" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ParameterMasters_Tenants_TenantId"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_ParameterMasters_TenantId_Code"
    ON "ParameterMasters" ("TenantId", "Code");

CREATE INDEX "IX_ParameterMasters_TenantId"
    ON "ParameterMasters" ("TenantId");

-- ============================================================
-- ROLE PARAMETER MAPPINGS
-- ============================================================
CREATE TABLE "RoleParameterMappings" (
    "Id"                UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID            NOT NULL,
    "DesignationCode"   VARCHAR(150)    NOT NULL,
    "ParameterId"       UUID            NOT NULL,
    "Weightage"         DECIMAL(5,2)    NOT NULL DEFAULT 0,
    "DisplayOrder"      INT             NOT NULL DEFAULT 0,
    "IsMandatory"       BOOLEAN         NOT NULL DEFAULT FALSE,
    "IsActive"          BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"       TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID            NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID            NOT NULL,
    "RecordVersion"     INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_RoleParameterMappings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RoleParameterMappings_Tenants_TenantId"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_RoleParameterMappings_ParameterMasters_ParameterId"
        FOREIGN KEY ("ParameterId") REFERENCES "ParameterMasters" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_RoleParameterMappings_TenantId_DesignationCode_ParameterId_Active"
    ON "RoleParameterMappings" ("TenantId", "DesignationCode", "ParameterId")
    WHERE "IsActive" = TRUE;

CREATE INDEX "IX_RoleParameterMappings_TenantId"
    ON "RoleParameterMappings" ("TenantId");

-- ============================================================
-- EMPLOYEE ASSESSMENTS
-- Core transactional table. One row per employee per period.
-- Status: 0=Draft, 1=Submitted, 2=Reopened
-- ============================================================
CREATE TABLE "EmployeeAssessments" (
    "Id"                    UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"              UUID            NOT NULL,
    "UserId"                UUID            NOT NULL,
    "GroupId"               UUID            NOT NULL,
    "AssessmentPeriodId"    UUID            NOT NULL,
    "RoleCode"              VARCHAR(150)    NOT NULL,
    "Designation"           VARCHAR(150),
    "RatingScaleId"         UUID            NOT NULL,
    "RatingValue"           INT             NOT NULL DEFAULT 0,
    "Comment"               TEXT,
    "EvidenceNotes"         TEXT,
    "ParameterSummaryJson"  JSONB,
    "Status"                INT             NOT NULL DEFAULT 0,
    "SubmittedBy"           UUID,
    "SubmittedOn"           TIMESTAMPTZ,
    "CreatedDate"           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"             UUID            NOT NULL,
    "ModifiedOn"            TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"            UUID            NOT NULL,
    "RecordVersion"         INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_EmployeeAssessments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeAssessments_Tenants_TenantId"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EmployeeAssessments_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EmployeeAssessments_AIAssessmentGroups_GroupId"
        FOREIGN KEY ("GroupId") REFERENCES "AIAssessmentGroups" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EmployeeAssessments_AssessmentPeriods_AssessmentPeriodId"
        FOREIGN KEY ("AssessmentPeriodId") REFERENCES "AssessmentPeriods" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EmployeeAssessments_RatingScales_RatingScaleId"
        FOREIGN KEY ("RatingScaleId") REFERENCES "RatingScales" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EmployeeAssessments_Users_SubmittedBy"
        FOREIGN KEY ("SubmittedBy") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_EmployeeAssessments_TenantId_UserId_PeriodId"
    ON "EmployeeAssessments" ("TenantId", "UserId", "AssessmentPeriodId");

CREATE INDEX "IX_EmployeeAssessments_TenantId_GroupId"
    ON "EmployeeAssessments" ("TenantId", "GroupId");

CREATE INDEX "IX_EmployeeAssessments_TenantId_PeriodId"
    ON "EmployeeAssessments" ("TenantId", "AssessmentPeriodId");

CREATE INDEX "IX_EmployeeAssessments_TenantId_Status"
    ON "EmployeeAssessments" ("TenantId", "Status");

CREATE INDEX "IX_EmployeeAssessments_TenantId"
    ON "EmployeeAssessments" ("TenantId");

-- ============================================================
-- EMPLOYEE ASSESSMENT PARAMETER DETAILS
-- Optional Phase 2 per-parameter sub-ratings.
-- ============================================================
CREATE TABLE "EmployeeAssessmentParameterDetails" (
    "Id"                        UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"                  UUID            NOT NULL,
    "EmployeeAssessmentId"      UUID            NOT NULL,
    "ParameterId"               UUID            NOT NULL,
    "ParameterRatingScaleId"    UUID            NOT NULL,
    "Comment"                   TEXT,
    "EvidenceNotes"             TEXT,
    "CreatedDate"               TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"                 UUID            NOT NULL,
    "ModifiedOn"                TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"                UUID            NOT NULL,
    "RecordVersion"             INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_EmployeeAssessmentParameterDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EAPDetails_Tenants_TenantId"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EAPDetails_EmployeeAssessments_AssessmentId"
        FOREIGN KEY ("EmployeeAssessmentId") REFERENCES "EmployeeAssessments" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EAPDetails_ParameterMasters_ParameterId"
        FOREIGN KEY ("ParameterId") REFERENCES "ParameterMasters" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EAPDetails_RatingScales_ParameterRatingScaleId"
        FOREIGN KEY ("ParameterRatingScaleId") REFERENCES "RatingScales" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_EAPDetails_TenantId_AssessmentId_ParameterId"
    ON "EmployeeAssessmentParameterDetails" ("TenantId", "EmployeeAssessmentId", "ParameterId");

CREATE INDEX "IX_EAPDetails_TenantId_AssessmentId"
    ON "EmployeeAssessmentParameterDetails" ("TenantId", "EmployeeAssessmentId");

-- ============================================================
-- ASSESSMENT AUDIT LOGS
-- Immutable audit trail for all state changes.
-- EmployeeAssessmentId is nullable for group-level changes.
-- ============================================================
CREATE TABLE "AssessmentAuditLogs" (
    "Id"                    UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"              UUID            NOT NULL,
    "EmployeeAssessmentId"  UUID,
    "RelatedEntityType"     VARCHAR(100)    NOT NULL,
    "RelatedEntityId"       UUID            NOT NULL,
    "ActionType"            INT             NOT NULL,
    "OldValueJson"          JSONB,
    "NewValueJson"          JSONB,
    "ChangedBy"             UUID            NOT NULL,
    "ChangedOn"             TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "Remarks"               TEXT,
    "CreatedDate"           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"             UUID            NOT NULL,
    "ModifiedOn"            TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"            UUID            NOT NULL,
    "RecordVersion"         INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_AssessmentAuditLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AssessmentAuditLogs_Tenants_TenantId"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AssessmentAuditLogs_Users_ChangedBy"
        FOREIGN KEY ("ChangedBy") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AssessmentAuditLogs_EmployeeAssessments_Id"
        FOREIGN KEY ("EmployeeAssessmentId") REFERENCES "EmployeeAssessments" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_AssessmentAuditLogs_TenantId_RelatedEntityId"
    ON "AssessmentAuditLogs" ("TenantId", "RelatedEntityId");

CREATE INDEX "IX_AssessmentAuditLogs_TenantId_EmployeeAssessmentId"
    ON "AssessmentAuditLogs" ("TenantId", "EmployeeAssessmentId")
    WHERE "EmployeeAssessmentId" IS NOT NULL;

CREATE INDEX "IX_AssessmentAuditLogs_TenantId_ChangedBy"
    ON "AssessmentAuditLogs" ("TenantId", "ChangedBy");

CREATE INDEX "IX_AssessmentAuditLogs_TenantId"
    ON "AssessmentAuditLogs" ("TenantId");

-- ============================================================
-- SEED DATA: Rating Scales (default 5-band scale)
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
         'Applying AI tools to complete role-specific tasks',
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
