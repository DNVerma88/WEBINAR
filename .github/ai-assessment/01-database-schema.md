-- ============================================================
-- KnowHub — Migration: 006_AIAssessmentModule.sql
-- AI Usage Rating / AI Maturity Assessment Module
-- PascalCase columns, UUID PKs, TIMESTAMPTZ, tenant-isolated
-- ============================================================

-- ============================================================
-- NEW ENUMS (stored as INT in column definitions)
-- AssessmentPeriodFrequency: Weekly=0, BiWeekly=1
-- AssessmentPeriodStatus:    Draft=0, Open=1, Closed=2, Published=3
-- AssessmentStatus:          Draft=0, Submitted=1, Reopened=2
-- AssessmentActionType:      Created=0, Updated=1, Submitted=2, Reopened=3,
--                            ChampionChanged=4, CoEAssigned=5, CoERemoved=6,
--                            EmployeeAssigned=7, EmployeeRemoved=8,
--                            PeriodOpened=9, PeriodClosed=10, PeriodPublished=11
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

-- Enforce: one Champion can lead at most one active group per tenant
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

-- Enforce: one employee can be active in only one group at a time per tenant
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

-- Enforce: a CoE can only be assigned once per active group
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

CREATE UNIQUE INDEX "IX_RatingScales_TenantId_NumericValue"
    ON "RatingScales" ("TenantId", "NumericValue")
    WHERE "IsActive" = TRUE;

CREATE INDEX "IX_RatingScales_TenantId"
    ON "RatingScales" ("TenantId");

-- ============================================================
-- RUBRIC DEFINITIONS
-- Role (Designation) + RatingScale → Behavior/Process/Evidence guidance.
-- Versioned: old record gets EffectiveTo set; new record inserted.
-- DesignationCode matches User.Designation string (case-insensitive).
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
-- PARAMETER MASTER
-- Configurable scoring parameters (Phase 2 detail scoring).
-- Category examples: AIAwareness, Prompting, TaskExecution,
--   ReviewValidation, WorkflowIntegration, ProductivityEvidence,
--   KnowledgeSharing, ResponsibleAI
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
-- Maps scoring parameters to designation codes with weightage.
-- Weightage values per designation should sum to 100.
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

CREATE UNIQUE INDEX "IX_RoleParameterMappings_TenantId_DesignationCode_ParameterId"
    ON "RoleParameterMappings" ("TenantId", "DesignationCode", "ParameterId")
    WHERE "IsActive" = TRUE;

CREATE INDEX "IX_RoleParameterMappings_TenantId"
    ON "RoleParameterMappings" ("TenantId");

-- ============================================================
-- EMPLOYEE ASSESSMENTS
-- Core transactional table. One row per employee per period.
-- Status: 0=Draft, 1=Submitted, 2=Reopened
-- RoleCode and Designation captured at time of assessment (snapshot).
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
    "RatingValue"           INT             NOT NULL,
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

-- Core uniqueness: one assessment per employee per period
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
-- Optional Phase 2: per-parameter sub-ratings.
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
-- ASSESSMENT AUDIT LOG
-- Immutable audit trail. RelatedEntityType + RelatedEntityId
-- identify what changed. ActionType uses AssessmentActionType enum.
-- EmployeeAssessmentId may be null for group-level changes.
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
