-- Migration 009: Talent Module
DO $$ BEGIN CREATE TYPE "ScreeningJobStatus" AS ENUM ('Pending', 'Processing', 'Completed', 'Failed', 'Cancelled'); EXCEPTION WHEN duplicate_object THEN null; END $$;
DO $$ BEGIN CREATE TYPE "CandidateStatus" AS ENUM ('Queued', 'Processing', 'Scored', 'Failed'); EXCEPTION WHEN duplicate_object THEN null; END $$;

CREATE TABLE IF NOT EXISTS "ResumeProfiles" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "UserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Template" VARCHAR(50) NOT NULL DEFAULT 'Professional',
    "PersonalInfo" JSONB NOT NULL DEFAULT '{}',
    "Summary" TEXT,
    "WorkExperience" JSONB NOT NULL DEFAULT '[]',
    "Education" JSONB NOT NULL DEFAULT '[]',
    "Skills" JSONB NOT NULL DEFAULT '[]',
    "Certifications" JSONB NOT NULL DEFAULT '[]',
    "Projects" JSONB NOT NULL DEFAULT '[]',
    "Languages" JSONB NOT NULL DEFAULT '[]',
    "Publications" JSONB NOT NULL DEFAULT '[]',
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE("TenantId", "UserId")
);

CREATE TABLE IF NOT EXISTS "ScreeningJobs" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "CreatedByUserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "JobTitle" VARCHAR(300) NOT NULL,
    "JdText" TEXT,
    "JdFileReference" JSONB,
    "JdEmbedding" JSONB,
    "Status" "ScreeningJobStatus" NOT NULL DEFAULT 'Pending',
    "TotalCandidates" INTEGER NOT NULL DEFAULT 0,
    "ProcessedCandidates" INTEGER NOT NULL DEFAULT 0,
    "ProgressPercent" INTEGER NOT NULL DEFAULT 0,
    "ErrorMessage" TEXT,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "StartedAt" TIMESTAMPTZ,
    "CompletedAt" TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS "IX_ScreeningJobs_TenantId" ON "ScreeningJobs"("TenantId");
CREATE INDEX IF NOT EXISTS "IX_ScreeningJobs_CreatedByUserId" ON "ScreeningJobs"("CreatedByUserId");
CREATE INDEX IF NOT EXISTS "IX_ScreeningJobs_Status" ON "ScreeningJobs"("Status");

CREATE TABLE IF NOT EXISTS "ScreeningCandidates" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ScreeningJobId" UUID NOT NULL REFERENCES "ScreeningJobs"("Id") ON DELETE CASCADE,
    "FileName" VARCHAR(500) NOT NULL,
    "StorageProviderType" VARCHAR(50) NOT NULL DEFAULT 'Local',
    "FileReference" TEXT NOT NULL,
    "Status" "CandidateStatus" NOT NULL DEFAULT 'Queued',
    "ErrorMessage" TEXT,
    "ExtractedText" TEXT,
    "CandidateName" VARCHAR(300),
    "Email" VARCHAR(300),
    "Phone" VARCHAR(100),
    "SemanticSimilarityScore" DECIMAL(5,4),
    "SkillsDepthScore" DECIMAL(5,2),
    "LegitimacyScore" DECIMAL(5,2),
    "OverallScore" DECIMAL(5,2),
    "Recommendation" VARCHAR(50),
    "ScoreSummary" TEXT,
    "SkillsMatched" JSONB,
    "SkillsGap" JSONB,
    "RedFlags" JSONB,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ScoredAt" TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS "IX_ScreeningCandidates_JobId" ON "ScreeningCandidates"("ScreeningJobId");
CREATE INDEX IF NOT EXISTS "IX_ScreeningCandidates_JobId_Score" ON "ScreeningCandidates"("ScreeningJobId", "OverallScore" DESC NULLS LAST);
