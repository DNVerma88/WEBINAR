-- ─────────────────────────────────────────────────────────────────
-- 018: Performance Fixes — indexes, duplicate cleanup, covering indexes
-- ─────────────────────────────────────────────────────────────────

-- ── 1. Drop duplicate indexes ─────────────────────────────────────

-- SurveyInvitations.TokenHash: the UNIQUE constraint already creates a B-tree
-- index; the explicit IX_ index below is a duplicate consuming extra write overhead.
DROP INDEX IF EXISTS "IX_SurveyInvitations_TokenHash";

-- ── 2. Replace unscoped indexes with TenantId-prefixed equivalents ─

-- MentorMentees: Status-only index is cross-tenant
DROP INDEX IF EXISTS "IX_MentorMentees_Status";
CREATE INDEX IF NOT EXISTS "IX_MentorMentees_TenantId_Status"
    ON "MentorMentees" ("TenantId", "Status");

-- KnowledgeAssetReviews: unscoped Status and ReviewerId indexes
DROP INDEX IF EXISTS "IX_KnowledgeAssetReviews_Status";
DROP INDEX IF EXISTS "IX_KnowledgeAssetReviews_ReviewerId";
CREATE INDEX IF NOT EXISTS "IX_KnowledgeAssetReviews_TenantId_Status"
    ON "KnowledgeAssetReviews" ("TenantId", "Status");
CREATE INDEX IF NOT EXISTS "IX_KnowledgeAssetReviews_TenantId_ReviewerId"
    ON "KnowledgeAssetReviews" ("TenantId", "ReviewerId");

-- ScreeningJobs: unscoped Status index (TenantId_CreatedAt already added in 009)
DROP INDEX IF EXISTS "IX_ScreeningJobs_Status";
CREATE INDEX IF NOT EXISTS "IX_ScreeningJobs_TenantId_Status"
    ON "ScreeningJobs" ("TenantId", "Status");

-- SpeakerAvailability: unscoped AvailableFrom index
DROP INDEX IF EXISTS "IX_SpeakerAvailability_AvailableFrom";
CREATE INDEX IF NOT EXISTS "IX_SpeakerAvailability_TenantId_AvailableFrom"
    ON "SpeakerAvailability" ("TenantId", "AvailableFrom");

-- EmailLogs: replace three unscoped indexes with TenantId-prefixed composite ones
DROP INDEX IF EXISTS "IX_EmailLogs_RecipientUserId";
DROP INDEX IF EXISTS "IX_EmailLogs_SentAt";
DROP INDEX IF EXISTS "IX_EmailLogs_EmailType";
CREATE INDEX IF NOT EXISTS "IX_EmailLogs_TenantId_RecipientUserId"
    ON "EmailLogs" ("TenantId", "RecipientUserId");
CREATE INDEX IF NOT EXISTS "IX_EmailLogs_TenantId_SentAt"
    ON "EmailLogs" ("TenantId", "SentAt" DESC);
CREATE INDEX IF NOT EXISTS "IX_EmailLogs_TenantId_EmailType_Status"
    ON "EmailLogs" ("TenantId", "EmailType", "Status");

-- PostViewEvents: replace unscoped PostId_ViewedAt with TenantId-prefixed version
DROP INDEX IF EXISTS "IX_PostViewEvents_PostId_ViewedAt";
CREATE INDEX IF NOT EXISTS "IX_PostViewEvents_TenantId_PostId_ViewedAt"
    ON "PostViewEvents" ("TenantId", "PostId", "ViewedAt" DESC);
CREATE INDEX IF NOT EXISTS "IX_PostViewEvents_ViewerId"
    ON "PostViewEvents" ("ViewerId") WHERE "ViewerId" IS NOT NULL;

-- ── 3. Authentication hot-path ────────────────────────────────────

-- Users.RefreshTokenHash: queried on every token refresh — no index causes full scan
CREATE INDEX IF NOT EXISTS "IX_Users_RefreshTokenHash"
    ON "Users" ("RefreshTokenHash") WHERE "RefreshTokenHash" IS NOT NULL;

-- ── 4. Missing FK indexes ─────────────────────────────────────────

-- AssessmentGroupMembers.WorkRoleId — added in migration 007 without index
CREATE INDEX IF NOT EXISTS "IX_AssessmentGroupMembers_WorkRoleId"
    ON "AssessmentGroupMembers" ("WorkRoleId") WHERE "WorkRoleId" IS NOT NULL;

-- UserBadges.BadgeId — FK without index
CREATE INDEX IF NOT EXISTS "IX_UserBadges_TenantId_BadgeId"
    ON "UserBadges" ("TenantId", "BadgeId");

-- Comments.ParentCommentId — self-referencing FK for threaded comments
CREATE INDEX IF NOT EXISTS "IX_Comments_TenantId_ParentCommentId"
    ON "Comments" ("TenantId", "ParentCommentId") WHERE "ParentCommentId" IS NOT NULL;

-- PostComments.ParentCommentId — self-referencing FK for threaded post comments
CREATE INDEX IF NOT EXISTS "IX_PostComments_TenantId_ParentCommentId"
    ON "PostComments" ("TenantId", "ParentCommentId") WHERE "ParentCommentId" IS NOT NULL;

-- LearningPathItems reverse FK indexes — "which paths include this session/asset?"
CREATE INDEX IF NOT EXISTS "IX_LearningPathItems_TenantId_SessionId"
    ON "LearningPathItems" ("TenantId", "SessionId") WHERE "SessionId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_LearningPathItems_TenantId_KnowledgeAssetId"
    ON "LearningPathItems" ("TenantId", "KnowledgeAssetId") WHERE "KnowledgeAssetId" IS NOT NULL;

-- CommunityWikiPages.ParentPageId — hierarchical tree traversal
CREATE INDEX IF NOT EXISTS "IX_CommunityWikiPages_TenantId_ParentPageId"
    ON "CommunityWikiPages" ("TenantId", "ParentPageId") WHERE "ParentPageId" IS NOT NULL;

-- ContentReports target FK indexes
CREATE INDEX IF NOT EXISTS "IX_ContentReports_TargetPostId"
    ON "ContentReports" ("TargetPostId") WHERE "TargetPostId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_ContentReports_TargetCommentId"
    ON "ContentReports" ("TargetCommentId") WHERE "TargetCommentId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_ContentReports_TenantId_ReporterId"
    ON "ContentReports" ("TenantId", "ReporterId");

-- ── 5. Reverse-lookup indexes ─────────────────────────────────────

-- CommunityMembers: "which communities does user X belong to?"
CREATE INDEX IF NOT EXISTS "IX_CommunityMembers_TenantId_UserId"
    ON "CommunityMembers" ("TenantId", "UserId");

-- SessionTags: "which sessions have tag X?" (reverse of existing TenantId_SessionId_TagId)
CREATE INDEX IF NOT EXISTS "IX_SessionTags_TenantId_TagId"
    ON "SessionTags" ("TenantId", "TagId");

-- ── 6. User filter indexes ────────────────────────────────────────

CREATE INDEX IF NOT EXISTS "IX_Users_TenantId_IsActive"
    ON "Users" ("TenantId", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_Users_TenantId_Role"
    ON "Users" ("TenantId", "Role");

-- ── 7. ContributorProfiles discovery indexes ──────────────────────

CREATE INDEX IF NOT EXISTS "IX_ContributorProfiles_TenantId_AvailableForMentoring"
    ON "ContributorProfiles" ("TenantId", "AvailableForMentoring")
    WHERE "AvailableForMentoring" = TRUE;
CREATE INDEX IF NOT EXISTS "IX_ContributorProfiles_TenantId_IsKnowledgeBroker"
    ON "ContributorProfiles" ("TenantId", "IsKnowledgeBroker")
    WHERE "IsKnowledgeBroker" = TRUE;

-- ── 8. Sessions catalog indexes ───────────────────────────────────

CREATE INDEX IF NOT EXISTS "IX_Sessions_TenantId_CategoryId"
    ON "Sessions" ("TenantId", "CategoryId");
CREATE INDEX IF NOT EXISTS "IX_Sessions_TenantId_Status_ScheduledAt"
    ON "Sessions" ("TenantId", "Status", "ScheduledAt");

-- ── 9. KnowledgeAssets browse indexes ────────────────────────────

CREATE INDEX IF NOT EXISTS "IX_KnowledgeAssets_TenantId_AssetType"
    ON "KnowledgeAssets" ("TenantId", "AssetType");
CREATE INDEX IF NOT EXISTS "IX_KnowledgeAssets_TenantId_IsPublic"
    ON "KnowledgeAssets" ("TenantId", "IsPublic");
CREATE INDEX IF NOT EXISTS "IX_KnowledgeAssets_TenantId_IsVerified"
    ON "KnowledgeAssets" ("TenantId", "IsVerified");

-- ── 10. KnowledgeRequests board indexes ──────────────────────────

CREATE INDEX IF NOT EXISTS "IX_KnowledgeRequests_TenantId_IsAddressed"
    ON "KnowledgeRequests" ("TenantId", "IsAddressed");
CREATE INDEX IF NOT EXISTS "IX_KnowledgeRequests_TenantId_UpvoteCount"
    ON "KnowledgeRequests" ("TenantId", "UpvoteCount" DESC);

-- ── 11. Partial index: active suspensions (checked on every auth) ─

CREATE INDEX IF NOT EXISTS "IX_UserSuspensions_TenantId_UserId_Active"
    ON "UserSuspensions" ("TenantId", "UserId")
    WHERE "IsActive" = TRUE;

-- ── 12. Partial index: unread notifications (polled every 60s) ────

CREATE INDEX IF NOT EXISTS "IX_Notifications_TenantId_UserId_Unread"
    ON "Notifications" ("TenantId", "UserId", "CreatedDate" DESC)
    WHERE "IsRead" = FALSE;

-- ── 13. LearningPaths & Cohorts ───────────────────────────────────

CREATE INDEX IF NOT EXISTS "IX_LearningPaths_TenantId_IsPublished"
    ON "LearningPaths" ("TenantId", "IsPublished")
    WHERE "IsPublished" = TRUE;

CREATE INDEX IF NOT EXISTS "IX_LearningPathCohorts_TenantId_Status"
    ON "LearningPathCohorts" ("TenantId", "Status");
CREATE INDEX IF NOT EXISTS "IX_LearningPathCohorts_TenantId_StartDate"
    ON "LearningPathCohorts" ("TenantId", "StartDate");

-- ── 14. Surveys.EndsAt — added in migration 014, never indexed ────

CREATE INDEX IF NOT EXISTS "IX_Surveys_TenantId_EndsAt"
    ON "Surveys" ("TenantId", "EndsAt") WHERE "EndsAt" IS NOT NULL;

-- ── 15. Assessment module indexes ────────────────────────────────

CREATE INDEX IF NOT EXISTS "IX_WorkRoles_TenantId"
    ON "WorkRoles" ("TenantId");
CREATE INDEX IF NOT EXISTS "IX_WorkRoles_TenantId_IsActive"
    ON "WorkRoles" ("TenantId", "IsActive");

CREATE INDEX IF NOT EXISTS "IX_ParameterMasters_TenantId_Category"
    ON "ParameterMasters" ("TenantId", "Category");

CREATE INDEX IF NOT EXISTS "IX_RoleParameterMappings_TenantId_DesignationCode"
    ON "RoleParameterMappings" ("TenantId", "DesignationCode");

-- AssessmentAuditLogs time-scoped index for recent-changes queries
CREATE INDEX IF NOT EXISTS "IX_AssessmentAuditLogs_TenantId_ChangedOn"
    ON "AssessmentAuditLogs" ("TenantId", "ChangedOn" DESC);

-- ── 16. UserXpEvents covering index (append-only ledger) ──────────

CREATE INDEX IF NOT EXISTS "IX_UserXpEvents_TenantId_UserId_EarnedAt"
    ON "UserXpEvents" ("TenantId", "UserId", "EarnedAt" DESC)
    INCLUDE ("XpAmount", "EventType");

-- ── 17. CommunityPosts feature/series indexes ─────────────────────

CREATE INDEX IF NOT EXISTS "IX_CommunityPosts_TenantId_IsFeatured"
    ON "CommunityPosts" ("TenantId", "IsFeatured", "PublishedAt" DESC)
    WHERE "IsFeatured" = TRUE;
CREATE INDEX IF NOT EXISTS "IX_CommunityPosts_TenantId_SeriesId"
    ON "CommunityPosts" ("TenantId", "SeriesId", "SeriesOrder")
    WHERE "SeriesId" IS NOT NULL;

CREATE INDEX IF NOT EXISTS "IX_PostSeries_TenantId_CommunityId"
    ON "PostSeries" ("TenantId", "CommunityId");
CREATE INDEX IF NOT EXISTS "IX_PostSeries_TenantId_AuthorId"
    ON "PostSeries" ("TenantId", "AuthorId");
