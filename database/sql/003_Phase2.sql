-- ============================================================
-- KnowHub Phase 2 Migration
-- Phase 2: Content, Engagement & Learning
-- ============================================================

-- ============================================================
-- UserXpEvents (append-only XP ledger)
-- ============================================================
CREATE TABLE "UserXpEvents" (
    "Id"                UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID          NOT NULL,
    "UserId"            UUID          NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "EventType"         VARCHAR(50)   NOT NULL,
    "XpAmount"          INT           NOT NULL,
    "RelatedEntityType" VARCHAR(50)   NULL,
    "RelatedEntityId"   UUID          NULL,
    "EarnedAt"          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedDate"       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID          NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID          NOT NULL,
    "RecordVersion"     INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_UserXpEvents" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_UserXpEvents_TenantId" ON "UserXpEvents"("TenantId");
CREATE INDEX "IX_UserXpEvents_TenantId_UserId" ON "UserXpEvents"("TenantId", "UserId");
CREATE INDEX "IX_UserXpEvents_EarnedAt" ON "UserXpEvents"("EarnedAt");

-- ============================================================
-- LearningPaths
-- ============================================================
CREATE TABLE "LearningPaths" (
    "Id"                        UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"                  UUID          NOT NULL,
    "Title"                     VARCHAR(200)  NOT NULL,
    "Slug"                      VARCHAR(200)  NOT NULL,
    "Description"               TEXT          NULL,
    "Objective"                 TEXT          NULL,
    "CategoryId"                UUID          NULL REFERENCES "Categories"("Id") ON DELETE RESTRICT,
    "DifficultyLevel"           VARCHAR(20)   NOT NULL,
    "EstimatedDurationMinutes"  INT           NOT NULL DEFAULT 0,
    "IsPublished"               BOOLEAN       NOT NULL DEFAULT FALSE,
    "IsAssignable"              BOOLEAN       NOT NULL DEFAULT TRUE,
    "CoverImageUrl"             VARCHAR(500)  NULL,
    "CreatedDate"               TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"                 UUID          NOT NULL,
    "ModifiedOn"                TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"                UUID          NOT NULL,
    "RecordVersion"             INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_LearningPaths" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_LearningPaths_TenantId_Slug" ON "LearningPaths"("TenantId", "Slug");
CREATE INDEX "IX_LearningPaths_TenantId" ON "LearningPaths"("TenantId");
CREATE INDEX "IX_LearningPaths_TenantId_CategoryId" ON "LearningPaths"("TenantId", "CategoryId");

-- ============================================================
-- LearningPathItems
-- ============================================================
CREATE TABLE "LearningPathItems" (
    "Id"                UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID          NOT NULL,
    "LearningPathId"    UUID          NOT NULL REFERENCES "LearningPaths"("Id") ON DELETE CASCADE,
    "ItemType"          VARCHAR(20)   NOT NULL,
    "SessionId"         UUID          NULL REFERENCES "Sessions"("Id") ON DELETE RESTRICT,
    "KnowledgeAssetId"  UUID          NULL REFERENCES "KnowledgeAssets"("Id") ON DELETE RESTRICT,
    "OrderSequence"     INT           NOT NULL,
    "IsRequired"        BOOLEAN       NOT NULL DEFAULT TRUE,
    "CreatedDate"       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID          NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID          NOT NULL,
    "RecordVersion"     INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_LearningPathItems" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_LearningPathItems_OneItemRef" CHECK (
        ("SessionId" IS NOT NULL AND "KnowledgeAssetId" IS NULL) OR
        ("SessionId" IS NULL AND "KnowledgeAssetId" IS NOT NULL)
    )
);
CREATE INDEX "IX_LearningPathItems_TenantId" ON "LearningPathItems"("TenantId");
CREATE INDEX "IX_LearningPathItems_TenantId_LearningPathId" ON "LearningPathItems"("TenantId", "LearningPathId");

-- ============================================================
-- UserLearningPathEnrollments
-- ============================================================
CREATE TABLE "UserLearningPathEnrollments" (
    "Id"                   UUID           NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"             UUID           NOT NULL,
    "UserId"               UUID           NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "LearningPathId"       UUID           NOT NULL REFERENCES "LearningPaths"("Id") ON DELETE RESTRICT,
    "EnrolmentType"        VARCHAR(30)    NOT NULL,
    "ProgressPercentage"   DECIMAL(5,2)   NOT NULL DEFAULT 0,
    "CompletedItemCount"   INT            NOT NULL DEFAULT 0,
    "DeadlineAt"           TIMESTAMPTZ    NULL,
    "StartedAt"            TIMESTAMPTZ    NULL,
    "CompletedAt"          TIMESTAMPTZ    NULL,
    "CreatedDate"          TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    "CreatedBy"            UUID           NOT NULL,
    "ModifiedOn"           TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    "ModifiedBy"           UUID           NOT NULL,
    "RecordVersion"        INT            NOT NULL DEFAULT 1,
    CONSTRAINT "PK_UserLearningPathEnrollments" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_UserLearningPathEnrollments_TenantId_UserId_PathId" ON "UserLearningPathEnrollments"("TenantId", "UserId", "LearningPathId");
CREATE INDEX "IX_UserLearningPathEnrollments_TenantId" ON "UserLearningPathEnrollments"("TenantId");
CREATE INDEX "IX_UserLearningPathEnrollments_TenantId_UserId" ON "UserLearningPathEnrollments"("TenantId", "UserId");

-- ============================================================
-- LearningPathCertificates
-- ============================================================
CREATE TABLE "LearningPathCertificates" (
    "Id"                UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID          NOT NULL,
    "UserId"            UUID          NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "LearningPathId"    UUID          NOT NULL REFERENCES "LearningPaths"("Id") ON DELETE RESTRICT,
    "CertificateNumber" VARCHAR(64)   NOT NULL,
    "CertificateUrl"    VARCHAR(500)  NOT NULL DEFAULT '',
    "IssuedAt"          TIMESTAMPTZ   NOT NULL,
    "CreatedDate"       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID          NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID          NOT NULL,
    "RecordVersion"     INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_LearningPathCertificates" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_LearningPathCertificates_CertificateNumber" ON "LearningPathCertificates"("CertificateNumber");
CREATE INDEX "IX_LearningPathCertificates_TenantId" ON "LearningPathCertificates"("TenantId");
CREATE INDEX "IX_LearningPathCertificates_TenantId_UserId" ON "LearningPathCertificates"("TenantId", "UserId");

-- ============================================================
-- SessionQuizzes
-- ============================================================
CREATE TABLE "SessionQuizzes" (
    "Id"                        UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"                  UUID          NOT NULL,
    "SessionId"                 UUID          NOT NULL UNIQUE REFERENCES "Sessions"("Id") ON DELETE CASCADE,
    "Title"                     VARCHAR(200)  NOT NULL,
    "Description"               TEXT          NULL,
    "PassingThresholdPercent"   INT           NOT NULL DEFAULT 70,
    "AllowRetry"                BOOLEAN       NOT NULL DEFAULT TRUE,
    "MaxAttempts"               INT           NOT NULL DEFAULT 2,
    "IsActive"                  BOOLEAN       NOT NULL DEFAULT TRUE,
    "CreatedDate"               TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"                 UUID          NOT NULL,
    "ModifiedOn"                TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"                UUID          NOT NULL,
    "RecordVersion"             INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_SessionQuizzes" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_SessionQuizzes_TenantId" ON "SessionQuizzes"("TenantId");
CREATE INDEX "IX_SessionQuizzes_TenantId_SessionId" ON "SessionQuizzes"("TenantId", "SessionId");

-- ============================================================
-- QuizQuestions
-- ============================================================
CREATE TABLE "QuizQuestions" (
    "Id"              UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"        UUID          NOT NULL,
    "QuizId"          UUID          NOT NULL REFERENCES "SessionQuizzes"("Id") ON DELETE CASCADE,
    "QuestionText"    TEXT          NOT NULL,
    "QuestionType"    VARCHAR(20)   NOT NULL,
    "Options"         JSONB         NULL,
    "CorrectAnswer"   VARCHAR(500)  NULL,
    "OrderSequence"   INT           NOT NULL,
    "Points"          INT           NOT NULL DEFAULT 1,
    "CreatedDate"     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"       UUID          NOT NULL,
    "ModifiedOn"      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"      UUID          NOT NULL,
    "RecordVersion"   INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_QuizQuestions" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_QuizQuestions_TenantId" ON "QuizQuestions"("TenantId");
CREATE INDEX "IX_QuizQuestions_TenantId_QuizId" ON "QuizQuestions"("TenantId", "QuizId");

-- ============================================================
-- UserQuizAttempts
-- ============================================================
CREATE TABLE "UserQuizAttempts" (
    "Id"              UUID           NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"        UUID           NOT NULL,
    "QuizId"          UUID           NOT NULL REFERENCES "SessionQuizzes"("Id") ON DELETE CASCADE,
    "UserId"          UUID           NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "AttemptNumber"   INT            NOT NULL,
    "Answers"         JSONB          NOT NULL DEFAULT '[]',
    "Score"           DECIMAL(5,2)   NULL,
    "IsPassed"        BOOLEAN        NULL,
    "SubmittedAt"     TIMESTAMPTZ    NOT NULL,
    "GradedAt"        TIMESTAMPTZ    NULL,
    "CreatedDate"     TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    "CreatedBy"       UUID           NOT NULL,
    "ModifiedOn"      TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    "ModifiedBy"      UUID           NOT NULL,
    "RecordVersion"   INT            NOT NULL DEFAULT 1,
    CONSTRAINT "PK_UserQuizAttempts" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_UserQuizAttempts_TenantId_QuizId_UserId_AttemptNumber" ON "UserQuizAttempts"("TenantId", "QuizId", "UserId", "AttemptNumber");
CREATE INDEX "IX_UserQuizAttempts_TenantId" ON "UserQuizAttempts"("TenantId");
CREATE INDEX "IX_UserQuizAttempts_TenantId_UserId" ON "UserQuizAttempts"("TenantId", "UserId");

-- ============================================================
-- SkillEndorsements
-- ============================================================
CREATE TABLE "SkillEndorsements" (
    "Id"            UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID          NOT NULL,
    "EndorserId"    UUID          NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "EndorseeId"    UUID          NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "TagId"         UUID          NOT NULL REFERENCES "Tags"("Id") ON DELETE RESTRICT,
    "SessionId"     UUID          NOT NULL REFERENCES "Sessions"("Id") ON DELETE RESTRICT,
    "EndorsedAt"    TIMESTAMPTZ   NOT NULL,
    "CreatedDate"   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID          NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID          NOT NULL,
    "RecordVersion" INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_SkillEndorsements" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_SkillEndorsements_Unique" ON "SkillEndorsements"("TenantId", "EndorserId", "EndorseeId", "TagId", "SessionId");
CREATE INDEX "IX_SkillEndorsements_TenantId" ON "SkillEndorsements"("TenantId");
CREATE INDEX "IX_SkillEndorsements_TenantId_EndorseeId" ON "SkillEndorsements"("TenantId", "EndorseeId");

-- ============================================================
-- CommunityWikiPages
-- ============================================================
CREATE TABLE "CommunityWikiPages" (
    "Id"                UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID          NOT NULL,
    "CommunityId"       UUID          NOT NULL REFERENCES "Communities"("Id") ON DELETE CASCADE,
    "AuthorId"          UUID          NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "Title"             VARCHAR(200)  NOT NULL,
    "Slug"              VARCHAR(200)  NOT NULL,
    "ContentMarkdown"   TEXT          NOT NULL DEFAULT '',
    "ParentPageId"      UUID          NULL REFERENCES "CommunityWikiPages"("Id") ON DELETE RESTRICT,
    "OrderSequence"     INT           NOT NULL DEFAULT 0,
    "IsPublished"       BOOLEAN       NOT NULL DEFAULT FALSE,
    "ViewCount"         INT           NOT NULL DEFAULT 0,
    "CreatedDate"       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID          NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID          NOT NULL,
    "RecordVersion"     INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_CommunityWikiPages" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_CommunityWikiPages_TenantId_CommunityId_Slug" ON "CommunityWikiPages"("TenantId", "CommunityId", "Slug");
CREATE INDEX "IX_CommunityWikiPages_TenantId" ON "CommunityWikiPages"("TenantId");
CREATE INDEX "IX_CommunityWikiPages_TenantId_CommunityId" ON "CommunityWikiPages"("TenantId", "CommunityId");

-- ============================================================
-- SessionChapters
-- ============================================================
CREATE TABLE "SessionChapters" (
    "Id"                UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID          NOT NULL,
    "SessionId"         UUID          NOT NULL REFERENCES "Sessions"("Id") ON DELETE CASCADE,
    "Title"             VARCHAR(200)  NOT NULL,
    "TimestampSeconds"  INT           NOT NULL,
    "OrderSequence"     INT           NOT NULL,
    "CreatedDate"       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID          NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID          NOT NULL,
    "RecordVersion"     INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_SessionChapters" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_SessionChapters_TenantId" ON "SessionChapters"("TenantId");
CREATE INDEX "IX_SessionChapters_TenantId_SessionId" ON "SessionChapters"("TenantId", "SessionId");

-- ============================================================
-- KnowledgeBundles
-- ============================================================
CREATE TABLE "KnowledgeBundles" (
    "Id"                UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID          NOT NULL,
    "Title"             VARCHAR(200)  NOT NULL,
    "Description"       TEXT          NULL,
    "CreatedByUserId"   UUID          NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "CategoryId"        UUID          NULL REFERENCES "Categories"("Id") ON DELETE RESTRICT,
    "IsPublished"       BOOLEAN       NOT NULL DEFAULT FALSE,
    "CoverImageUrl"     VARCHAR(500)  NULL,
    "CreatedDate"       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID          NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID          NOT NULL,
    "RecordVersion"     INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_KnowledgeBundles" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_KnowledgeBundles_TenantId" ON "KnowledgeBundles"("TenantId");
CREATE INDEX "IX_KnowledgeBundles_TenantId_CategoryId" ON "KnowledgeBundles"("TenantId", "CategoryId");

-- ============================================================
-- KnowledgeBundleItems
-- ============================================================
CREATE TABLE "KnowledgeBundleItems" (
    "Id"                UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID          NOT NULL,
    "BundleId"          UUID          NOT NULL REFERENCES "KnowledgeBundles"("Id") ON DELETE CASCADE,
    "KnowledgeAssetId"  UUID          NOT NULL REFERENCES "KnowledgeAssets"("Id") ON DELETE RESTRICT,
    "OrderSequence"     INT           NOT NULL,
    "Notes"             VARCHAR(500)  NULL,
    "CreatedDate"       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID          NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID          NOT NULL,
    "RecordVersion"     INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_KnowledgeBundleItems" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_KnowledgeBundleItems_TenantId_BundleId_AssetId" ON "KnowledgeBundleItems"("TenantId", "BundleId", "KnowledgeAssetId");
CREATE INDEX "IX_KnowledgeBundleItems_TenantId" ON "KnowledgeBundleItems"("TenantId");
CREATE INDEX "IX_KnowledgeBundleItems_TenantId_BundleId" ON "KnowledgeBundleItems"("TenantId", "BundleId");

-- ============================================================
-- AfterActionReviews
-- ============================================================
CREATE TABLE "AfterActionReviews" (
    "Id"                UUID        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID        NOT NULL,
    "SessionId"         UUID        NOT NULL UNIQUE REFERENCES "Sessions"("Id") ON DELETE CASCADE,
    "AuthorId"          UUID        NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "WhatWasPlanned"    TEXT        NOT NULL DEFAULT '',
    "WhatHappened"      TEXT        NOT NULL DEFAULT '',
    "WhatWentWell"      TEXT        NOT NULL DEFAULT '',
    "WhatToImprove"     TEXT        NOT NULL DEFAULT '',
    "KeyLessonsLearned" TEXT        NOT NULL DEFAULT '',
    "IsPublished"       BOOLEAN     NOT NULL DEFAULT FALSE,
    "CreatedDate"       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID        NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID        NOT NULL,
    "RecordVersion"     INT         NOT NULL DEFAULT 1,
    CONSTRAINT "PK_AfterActionReviews" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_AfterActionReviews_TenantId" ON "AfterActionReviews"("TenantId");
CREATE INDEX "IX_AfterActionReviews_TenantId_SessionId" ON "AfterActionReviews"("TenantId", "SessionId");

-- ============================================================
-- MentorMentees
-- ============================================================
CREATE TABLE "MentorMentees" (
    "Id"            UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID          NOT NULL,
    "MentorId"      UUID          NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "MenteeId"      UUID          NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "Status"        VARCHAR(20)   NOT NULL DEFAULT 'Pending',
    "StartedAt"     TIMESTAMPTZ   NULL,
    "EndedAt"       TIMESTAMPTZ   NULL,
    "GoalsText"     TEXT          NULL,
    "MatchReason"   VARCHAR(500)  NULL,
    "CreatedDate"   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID          NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID          NOT NULL,
    "RecordVersion" INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_MentorMentees" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_MentorMentees_TenantId" ON "MentorMentees"("TenantId");
CREATE INDEX "IX_MentorMentees_TenantId_MentorId" ON "MentorMentees"("TenantId", "MentorId");
CREATE INDEX "IX_MentorMentees_TenantId_MenteeId" ON "MentorMentees"("TenantId", "MenteeId");
CREATE INDEX "IX_MentorMentees_Status" ON "MentorMentees"("Status");

-- ============================================================
-- UserLearningStreaks (one row per user per tenant)
-- ============================================================
CREATE TABLE "UserLearningStreaks" (
    "Id"                  UUID        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"            UUID        NOT NULL,
    "UserId"              UUID        NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "CurrentStreakDays"   INT         NOT NULL DEFAULT 0,
    "LongestStreakDays"   INT         NOT NULL DEFAULT 0,
    "LastActivityDate"    DATE        NOT NULL,
    "StreakFrozenUntil"   DATE        NULL,
    "CreatedDate"         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"           UUID        NOT NULL,
    "ModifiedOn"          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"          UUID        NOT NULL,
    "RecordVersion"       INT         NOT NULL DEFAULT 1,
    CONSTRAINT "PK_UserLearningStreaks" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_UserLearningStreaks_TenantId_UserId" ON "UserLearningStreaks"("TenantId", "UserId");
CREATE INDEX "IX_UserLearningStreaks_TenantId" ON "UserLearningStreaks"("TenantId");

-- ============================================================
-- LeaderboardSnapshots
-- ============================================================
CREATE TABLE "LeaderboardSnapshots" (
    "Id"                UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID          NOT NULL,
    "SnapshotMonth"     INT           NOT NULL,
    "SnapshotYear"      INT           NOT NULL,
    "LeaderboardType"   VARCHAR(30)   NOT NULL,
    "Entries"           JSONB         NOT NULL DEFAULT '[]',
    "GeneratedAt"       TIMESTAMPTZ   NOT NULL,
    "CreatedDate"       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID          NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID          NOT NULL,
    "RecordVersion"     INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_LeaderboardSnapshots" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_LeaderboardSnapshots_Unique" ON "LeaderboardSnapshots"("TenantId", "SnapshotMonth", "SnapshotYear", "LeaderboardType");
CREATE INDEX "IX_LeaderboardSnapshots_TenantId" ON "LeaderboardSnapshots"("TenantId");
