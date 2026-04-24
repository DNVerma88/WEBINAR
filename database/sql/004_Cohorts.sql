-- ============================================================
-- KnowHub Migration 004 - Learning Path Cohorts
-- ============================================================

CREATE TABLE "LearningPathCohorts" (
    "Id"              UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"        UUID          NOT NULL,
    "LearningPathId"  UUID          NOT NULL REFERENCES "LearningPaths"("Id") ON DELETE CASCADE,
    "Name"            VARCHAR(200)  NOT NULL,
    "Description"     TEXT          NULL,
    "StartDate"       TIMESTAMPTZ   NOT NULL,
    "EndDate"         TIMESTAMPTZ   NULL,
    "MaxParticipants" INT           NULL,
    "Status"          VARCHAR(20)   NOT NULL DEFAULT 'Scheduled',
    "CreatedDate"     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"       UUID          NOT NULL,
    "ModifiedOn"      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"      UUID          NOT NULL,
    "RecordVersion"   INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_LearningPathCohorts" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_LearningPathCohorts_TenantId" ON "LearningPathCohorts"("TenantId");
CREATE INDEX "IX_LearningPathCohorts_TenantId_LearningPathId" ON "LearningPathCohorts"("TenantId", "LearningPathId");
