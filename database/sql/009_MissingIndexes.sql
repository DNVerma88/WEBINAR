-- Migration 008: Targeted index additions identified during tech-debt audit
-- These fill gaps found after reviewing hot query paths in the application.

-- ScreeningCandidates: background service filters by (JobId, Status = 'Queued')
-- The existing IX_ScreeningCandidates_JobId covers JobId alone; this compound
-- index lets Postgres skip rows that are not Queued when scanning large jobs.
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_ScreeningCandidates_JobId_Status"
    ON "ScreeningCandidates" ("ScreeningJobId", "Status");

-- ScreeningJobs: tenant-scoped queries frequently order by CreatedAt
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_ScreeningJobs_TenantId_CreatedAt"
    ON "ScreeningJobs" ("TenantId", "CreatedAt" DESC);

-- ContentFlags: admin moderation panel filters by (TenantId, Status, ContentType)
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_ContentFlags_TenantId_Status"
    ON "ContentFlags" ("TenantId", "Status");

-- KnowledgeRequests: contributor dashboard filters by (TenantId, RequesterId)
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_KnowledgeRequests_TenantId_RequesterId"
    ON "KnowledgeRequests" ("TenantId", "RequesterId");
