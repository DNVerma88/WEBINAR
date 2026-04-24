-- ============================================================
-- 008_GeneralizeAssessmentModule.sql
-- Generalize the AI Assessment module into a generic Assessment module.
-- Renames tables, renames a column, adds AssessmentCategory,
-- and updates parameter category values.
-- NOTE: Integer enum values stored in AssessmentAuditLogs.ActionType
-- are intentionally unchanged (4=PrimaryLeadChanged, 5=CoLeadAssigned,
-- 6=CoLeadRemoved, 7=MemberAssigned, 8=MemberRemoved).
-- ============================================================

-- 1. Rename tables (remove the AI-specific prefix)
ALTER TABLE "AIAssessmentGroups"         RENAME TO "AssessmentGroups";
ALTER TABLE "AIAssessmentGroupEmployees" RENAME TO "AssessmentGroupMembers";
ALTER TABLE "AIAssessmentGroupCoEs"      RENAME TO "AssessmentGroupCoLeads";

-- 2. Rename ChampionUserId → PrimaryLeadUserId on the groups table
ALTER TABLE "AssessmentGroups" RENAME COLUMN "ChampionUserId" TO "PrimaryLeadUserId";

-- 3. Add optional AssessmentCategory column to AssessmentGroups
--    Allows each group to be tagged with the type of assessment it runs
--    (e.g. 'AI_MATURITY', 'MID_TERM_REVIEW', 'ANNUAL_PERFORMANCE', etc.)
ALTER TABLE "AssessmentGroups"
    ADD COLUMN IF NOT EXISTS "AssessmentCategory" VARCHAR(100) NULL;

-- 4. Update existing AI Maturity parameter categories to carry the
--    'AI_MATURITY' tag so they are clearly associated with the original
--    AI-specific assessment template.
UPDATE "ParameterMasters"
SET    Category = 'AI_MATURITY'
WHERE  Category IN (
    'AIAwareness',
    'Prompting',
    'TaskExecution',
    'ReviewValidation',
    'WorkflowIntegration',
    'ProductivityEvidence',
    'KnowledgeSharing',
    'ResponsibleAI'
);
