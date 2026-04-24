-- KnowHub Database Schema
-- Migration: 001_Init.sql
-- PascalCase column names, no underscores

-- ============================================================
-- TENANTS
-- ============================================================
CREATE TABLE "Tenants" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "Name"          VARCHAR(200)    NOT NULL,
    "Slug"          VARCHAR(100)    NOT NULL,
    "IsActive"      BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_Tenants" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_Tenants_Slug" ON "Tenants" ("Slug");

-- ============================================================
-- USERS
-- ============================================================
CREATE TABLE "Users" (
    "Id"                    UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"              UUID            NOT NULL,
    "FullName"              VARCHAR(200)    NOT NULL,
    "Email"                 VARCHAR(256)    NOT NULL,
    "PasswordHash"          VARCHAR(500)    NOT NULL,
    "Department"            VARCHAR(150),
    "Designation"           VARCHAR(150),
    "YearsOfExperience"     INT,
    "Location"              VARCHAR(150),
    "ProfilePhotoUrl"       VARCHAR(500),
    "Role"                  INT             NOT NULL DEFAULT 1,
    "IsActive"              BOOLEAN         NOT NULL DEFAULT TRUE,
    "RefreshTokenHash"      VARCHAR(500),
    "RefreshTokenExpiresAt" TIMESTAMPTZ,
    "CreatedDate"           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"             UUID            NOT NULL,
    "ModifiedOn"            TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"            UUID            NOT NULL,
    "RecordVersion"         INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_Users_TenantId_Email" ON "Users" ("TenantId", "Email");
CREATE INDEX "IX_Users_TenantId" ON "Users" ("TenantId");

-- ============================================================
-- CONTRIBUTOR PROFILES
-- ============================================================
CREATE TABLE "ContributorProfiles" (
    "Id"                    UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"              UUID            NOT NULL,
    "UserId"                UUID            NOT NULL,
    "AreasOfExpertise"      VARCHAR(1000),
    "TechnologiesKnown"     VARCHAR(1000),
    "Bio"                   TEXT,
    "AverageRating"         DECIMAL(3,2)    NOT NULL DEFAULT 0,
    "TotalSessionsDelivered" INT            NOT NULL DEFAULT 0,
    "FollowerCount"         INT             NOT NULL DEFAULT 0,
    "EndorsementScore"      DECIMAL(10,2)   NOT NULL DEFAULT 0,
    "IsKnowledgeBroker"     BOOLEAN         NOT NULL DEFAULT FALSE,
    "AvailableForMentoring" BOOLEAN         NOT NULL DEFAULT FALSE,
    "CreatedDate"           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"             UUID            NOT NULL,
    "ModifiedOn"            TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"            UUID            NOT NULL,
    "RecordVersion"         INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_ContributorProfiles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ContributorProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_ContributorProfiles_TenantId_UserId" ON "ContributorProfiles" ("TenantId", "UserId");
CREATE INDEX "IX_ContributorProfiles_TenantId" ON "ContributorProfiles" ("TenantId");

-- ============================================================
-- CATEGORIES
-- ============================================================
CREATE TABLE "Categories" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "Name"          VARCHAR(150)    NOT NULL,
    "Description"   VARCHAR(500),
    "IconName"      VARCHAR(100),
    "SortOrder"     INT             NOT NULL DEFAULT 0,
    "IsActive"      BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_Categories" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Categories_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_Categories_TenantId_Name" ON "Categories" ("TenantId", "Name");
CREATE INDEX "IX_Categories_TenantId" ON "Categories" ("TenantId");

-- ============================================================
-- TAGS
-- ============================================================
CREATE TABLE "Tags" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "Name"          VARCHAR(100)    NOT NULL,
    "Slug"          VARCHAR(100)    NOT NULL,
    "UsageCount"    INT             NOT NULL DEFAULT 0,
    "IsActive"      BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_Tags" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Tags_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_Tags_TenantId_Slug" ON "Tags" ("TenantId", "Slug");
CREATE INDEX "IX_Tags_TenantId" ON "Tags" ("TenantId");

-- ============================================================
-- USER SKILLS
-- ============================================================
CREATE TABLE "UserSkills" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "UserId"        UUID            NOT NULL,
    "TagId"         UUID            NOT NULL,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_UserSkills" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserSkills_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserSkills_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES "Tags" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_UserSkills_TenantId_UserId_TagId" ON "UserSkills" ("TenantId", "UserId", "TagId");

-- ============================================================
-- USER FOLLOWERS
-- ============================================================
CREATE TABLE "UserFollowers" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "FollowerId"    UUID            NOT NULL,
    "FollowedId"    UUID            NOT NULL,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_UserFollowers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserFollowers_Users_FollowerId" FOREIGN KEY ("FollowerId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserFollowers_Users_FollowedId" FOREIGN KEY ("FollowedId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_UserFollowers_TenantId_FollowerId_FollowedId" ON "UserFollowers" ("TenantId", "FollowerId", "FollowedId");
CREATE INDEX "IX_UserFollowers_TenantId_FollowedId" ON "UserFollowers" ("TenantId", "FollowedId");

-- ============================================================
-- BADGES
-- ============================================================
CREATE TABLE "Badges" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "Name"          VARCHAR(150)    NOT NULL,
    "Description"   VARCHAR(500),
    "IconUrl"       VARCHAR(500),
    "Criteria"      VARCHAR(1000),
    "BadgeCategory" VARCHAR(50)     NOT NULL,
    "XpGranted"     INT             NOT NULL DEFAULT 0,
    "IsActive"      BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_Badges" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Badges_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Badges_TenantId" ON "Badges" ("TenantId");

-- ============================================================
-- USER BADGES
-- ============================================================
CREATE TABLE "UserBadges" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "UserId"        UUID            NOT NULL,
    "BadgeId"       UUID            NOT NULL,
    "AwardedAt"     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "AwardReason"   VARCHAR(500),
    "XpGranted"     INT             NOT NULL DEFAULT 0,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_UserBadges" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserBadges_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserBadges_Badges_BadgeId" FOREIGN KEY ("BadgeId") REFERENCES "Badges" ("Id") ON DELETE Restrict
);

CREATE INDEX "IX_UserBadges_TenantId_UserId" ON "UserBadges" ("TenantId", "UserId");

-- ============================================================
-- COMMUNITIES
-- ============================================================
CREATE TABLE "Communities" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "Name"          VARCHAR(200)    NOT NULL,
    "Slug"          VARCHAR(200)    NOT NULL,
    "Description"   TEXT,
    "IconName"      VARCHAR(100),
    "CoverImageUrl" VARCHAR(500),
    "MemberCount"   INT             NOT NULL DEFAULT 0,
    "IsActive"      BOOLEAN         NOT NULL DEFAULT TRUE,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_Communities" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Communities_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_Communities_TenantId_Slug" ON "Communities" ("TenantId", "Slug");
CREATE INDEX "IX_Communities_TenantId" ON "Communities" ("TenantId");

-- ============================================================
-- COMMUNITY MEMBERS
-- ============================================================
CREATE TABLE "CommunityMembers" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "CommunityId"   UUID            NOT NULL,
    "UserId"        UUID            NOT NULL,
    "MemberRole"    INT             NOT NULL DEFAULT 0,
    "JoinedAt"      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_CommunityMembers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommunityMembers_Communities_CommunityId" FOREIGN KEY ("CommunityId") REFERENCES "Communities" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CommunityMembers_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_CommunityMembers_TenantId_CommunityId_UserId" ON "CommunityMembers" ("TenantId", "CommunityId", "UserId");

-- ============================================================
-- SESSION PROPOSALS
-- ============================================================
CREATE TABLE "SessionProposals" (
    "Id"                    UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"              UUID            NOT NULL,
    "ProposerId"            UUID            NOT NULL,
    "Title"                 VARCHAR(300)    NOT NULL,
    "CategoryId"            UUID            NOT NULL,
    "Topic"                 VARCHAR(300)    NOT NULL,
    "DepartmentRelevance"   VARCHAR(300),
    "Description"           TEXT,
    "ProblemStatement"      TEXT,
    "LearningOutcomes"      TEXT,
    "TargetAudience"        VARCHAR(500),
    "Format"                INT             NOT NULL,
    "Duration"              INT             NOT NULL,
    "PreferredDate"         DATE,
    "PreferredTime"         TIME,
    "DifficultyLevel"       INT             NOT NULL,
    "RelatedProject"        VARCHAR(300),
    "AllowRecording"        BOOLEAN         NOT NULL DEFAULT TRUE,
    "Status"                INT             NOT NULL DEFAULT 0,
    "SubmittedAt"           TIMESTAMPTZ,
    "CreatedDate"           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"             UUID            NOT NULL,
    "ModifiedOn"            TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"            UUID            NOT NULL,
    "RecordVersion"         INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_SessionProposals" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SessionProposals_Users_ProposerId" FOREIGN KEY ("ProposerId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SessionProposals_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_SessionProposals_TenantId" ON "SessionProposals" ("TenantId");
CREATE INDEX "IX_SessionProposals_TenantId_Status" ON "SessionProposals" ("TenantId", "Status");
CREATE INDEX "IX_SessionProposals_TenantId_ProposerId" ON "SessionProposals" ("TenantId", "ProposerId");
CREATE INDEX "IX_SessionProposals_SubmittedAt" ON "SessionProposals" ("SubmittedAt");

-- ============================================================
-- PROPOSAL APPROVALS
-- ============================================================
CREATE TABLE "ProposalApprovals" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "ProposalId"    UUID            NOT NULL,
    "ApproverId"    UUID            NOT NULL,
    "ApprovalStep"  INT             NOT NULL,
    "Decision"      INT             NOT NULL,
    "Comment"       VARCHAR(2000),
    "DecidedAt"     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_ProposalApprovals" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProposalApprovals_SessionProposals_ProposalId" FOREIGN KEY ("ProposalId") REFERENCES "SessionProposals" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ProposalApprovals_Users_ApproverId" FOREIGN KEY ("ApproverId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_ProposalApprovals_TenantId_ProposalId" ON "ProposalApprovals" ("TenantId", "ProposalId");
CREATE INDEX "IX_ProposalApprovals_DecidedAt" ON "ProposalApprovals" ("DecidedAt");

-- ============================================================
-- SESSIONS
-- ============================================================
CREATE TABLE "Sessions" (
    "Id"                UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID            NOT NULL,
    "ProposalId"        UUID            NOT NULL,
    "SpeakerId"         UUID            NOT NULL,
    "Title"             VARCHAR(300)    NOT NULL,
    "CategoryId"        UUID            NOT NULL,
    "Format"            INT             NOT NULL,
    "DifficultyLevel"   INT             NOT NULL,
    "ScheduledAt"       TIMESTAMPTZ     NOT NULL,
    "DurationMinutes"   INT             NOT NULL,
    "MeetingLink"       VARCHAR(500)    NOT NULL,
    "MeetingPlatform"   INT             NOT NULL,
    "ParticipantLimit"  INT,
    "Status"            INT             NOT NULL DEFAULT 0,
    "IsPublic"          BOOLEAN         NOT NULL DEFAULT TRUE,
    "RecordingUrl"      VARCHAR(500),
    "Description"       TEXT,
    "CreatedDate"       TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID            NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID            NOT NULL,
    "RecordVersion"     INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_Sessions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Sessions_SessionProposals_ProposalId" FOREIGN KEY ("ProposalId") REFERENCES "SessionProposals" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Sessions_Users_SpeakerId" FOREIGN KEY ("SpeakerId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Sessions_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Sessions_TenantId" ON "Sessions" ("TenantId");
CREATE INDEX "IX_Sessions_TenantId_Status" ON "Sessions" ("TenantId", "Status");
CREATE INDEX "IX_Sessions_TenantId_SpeakerId" ON "Sessions" ("TenantId", "SpeakerId");
CREATE INDEX "IX_Sessions_ScheduledAt" ON "Sessions" ("ScheduledAt");

-- ============================================================
-- SESSION TAGS
-- ============================================================
CREATE TABLE "SessionTags" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "SessionId"     UUID            NOT NULL,
    "TagId"         UUID            NOT NULL,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_SessionTags" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SessionTags_Sessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "Sessions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SessionTags_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES "Tags" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_SessionTags_TenantId_SessionId_TagId" ON "SessionTags" ("TenantId", "SessionId", "TagId");

-- ============================================================
-- SESSION MATERIALS
-- ============================================================
CREATE TABLE "SessionMaterials" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "SessionId"     UUID,
    "ProposalId"    UUID,
    "MaterialType"  INT             NOT NULL,
    "Title"         VARCHAR(300)    NOT NULL,
    "Url"           VARCHAR(500)    NOT NULL,
    "FileSizeBytes" BIGINT,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_SessionMaterials" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SessionMaterials_Sessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "Sessions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SessionMaterials_SessionProposals_ProposalId" FOREIGN KEY ("ProposalId") REFERENCES "SessionProposals" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_SessionMaterials_TenantId_SessionId" ON "SessionMaterials" ("TenantId", "SessionId");

-- ============================================================
-- SESSION REGISTRATIONS
-- ============================================================
CREATE TABLE "SessionRegistrations" (
    "Id"                UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID            NOT NULL,
    "SessionId"         UUID            NOT NULL,
    "ParticipantId"     UUID            NOT NULL,
    "WaitlistPosition"  INT,
    "RegisteredAt"      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "AttendedAt"        TIMESTAMPTZ,
    "Status"            INT             NOT NULL DEFAULT 0,
    "CreatedDate"       TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID            NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID            NOT NULL,
    "RecordVersion"     INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_SessionRegistrations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SessionRegistrations_Sessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "Sessions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SessionRegistrations_Users_ParticipantId" FOREIGN KEY ("ParticipantId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_SessionRegistrations_TenantId_SessionId_ParticipantId" ON "SessionRegistrations" ("TenantId", "SessionId", "ParticipantId") WHERE "Status" != 3;
CREATE INDEX "IX_SessionRegistrations_TenantId_ParticipantId" ON "SessionRegistrations" ("TenantId", "ParticipantId");
CREATE INDEX "IX_SessionRegistrations_RegisteredAt" ON "SessionRegistrations" ("RegisteredAt");

-- ============================================================
-- KNOWLEDGE ASSETS
-- ============================================================
CREATE TABLE "KnowledgeAssets" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "SessionId"     UUID,
    "AssetType"     INT             NOT NULL,
    "Title"         VARCHAR(300)    NOT NULL,
    "Url"           VARCHAR(500)    NOT NULL,
    "Description"   TEXT,
    "ViewCount"     INT             NOT NULL DEFAULT 0,
    "DownloadCount" INT             NOT NULL DEFAULT 0,
    "IsPublic"      BOOLEAN         NOT NULL DEFAULT TRUE,
    "VersionNumber" INT             NOT NULL DEFAULT 1,
    "ExpiresAt"     TIMESTAMPTZ,
    "IsVerified"    BOOLEAN         NOT NULL DEFAULT FALSE,
    "VerifiedById"  UUID,
    "VerifiedAt"    TIMESTAMPTZ,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_KnowledgeAssets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_KnowledgeAssets_Sessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "Sessions" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_KnowledgeAssets_Users_VerifiedById" FOREIGN KEY ("VerifiedById") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_KnowledgeAssets_TenantId" ON "KnowledgeAssets" ("TenantId");
CREATE INDEX "IX_KnowledgeAssets_TenantId_SessionId" ON "KnowledgeAssets" ("TenantId", "SessionId");

-- ============================================================
-- COMMENTS
-- ============================================================
CREATE TABLE "Comments" (
    "Id"            UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID            NOT NULL,
    "AuthorId"      UUID            NOT NULL,
    "SessionId"     UUID,
    "KnowledgeAssetId" UUID,
    "ParentCommentId" UUID,
    "Body"          TEXT            NOT NULL,
    "IsDeleted"     BOOLEAN         NOT NULL DEFAULT FALSE,
    "CreatedDate"   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID            NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID            NOT NULL,
    "RecordVersion" INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_Comments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Comments_Users_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Comments_Sessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "Sessions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Comments_KnowledgeAssets_KnowledgeAssetId" FOREIGN KEY ("KnowledgeAssetId") REFERENCES "KnowledgeAssets" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Comments_TenantId_SessionId" ON "Comments" ("TenantId", "SessionId");
CREATE INDEX "IX_Comments_TenantId_KnowledgeAssetId" ON "Comments" ("TenantId", "KnowledgeAssetId");

-- ============================================================
-- LIKES
-- ============================================================
CREATE TABLE "Likes" (
    "Id"                    UUID    NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"              UUID    NOT NULL,
    "UserId"                UUID    NOT NULL,
    "SessionId"             UUID,
    "KnowledgeAssetId"      UUID,
    "CommentId"             UUID,
    "KnowledgeRequestId"    UUID,
    "CreatedDate"           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"             UUID    NOT NULL,
    "ModifiedOn"            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"            UUID    NOT NULL,
    "RecordVersion"         INT     NOT NULL DEFAULT 1,
    CONSTRAINT "PK_Likes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Likes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Likes_TenantId_UserId_SessionId" ON "Likes" ("TenantId", "UserId", "SessionId");

-- ============================================================
-- SESSION RATINGS
-- ============================================================
CREATE TABLE "SessionRatings" (
    "Id"                    UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"              UUID            NOT NULL,
    "SessionId"             UUID            NOT NULL,
    "RaterId"               UUID            NOT NULL,
    "SessionScore"          INT             NOT NULL,
    "SpeakerScore"          INT             NOT NULL,
    "FeedbackText"          VARCHAR(2000),
    "NextSessionSuggestion" VARCHAR(500),
    "CreatedDate"           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"             UUID            NOT NULL,
    "ModifiedOn"            TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"            UUID            NOT NULL,
    "RecordVersion"         INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_SessionRatings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SessionRatings_Sessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "Sessions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SessionRatings_Users_RaterId" FOREIGN KEY ("RaterId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_SessionRatings_TenantId_SessionId_RaterId" ON "SessionRatings" ("TenantId", "SessionId", "RaterId");

-- ============================================================
-- KNOWLEDGE REQUESTS
-- ============================================================
CREATE TABLE "KnowledgeRequests" (
    "Id"                    UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"              UUID            NOT NULL,
    "RequesterId"           UUID            NOT NULL,
    "Title"                 VARCHAR(300)    NOT NULL,
    "Description"           TEXT            NOT NULL,
    "CategoryId"            UUID,
    "UpvoteCount"           INT             NOT NULL DEFAULT 0,
    "IsAddressed"           BOOLEAN         NOT NULL DEFAULT FALSE,
    "AddressedBySessionId"  UUID,
    "Status"                INT             NOT NULL DEFAULT 0,
    "BountyXp"              INT             NOT NULL DEFAULT 0,
    "ClaimedByUserId"       UUID,
    "CreatedDate"           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"             UUID            NOT NULL,
    "ModifiedOn"            TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"            UUID            NOT NULL,
    "RecordVersion"         INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_KnowledgeRequests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_KnowledgeRequests_Users_RequesterId" FOREIGN KEY ("RequesterId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_KnowledgeRequests_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_KnowledgeRequests_Sessions_AddressedBySessionId" FOREIGN KEY ("AddressedBySessionId") REFERENCES "Sessions" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_KnowledgeRequests_Users_ClaimedByUserId" FOREIGN KEY ("ClaimedByUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_KnowledgeRequests_TenantId" ON "KnowledgeRequests" ("TenantId");
CREATE INDEX "IX_KnowledgeRequests_TenantId_Status" ON "KnowledgeRequests" ("TenantId", "Status");
CREATE INDEX "IX_KnowledgeRequests_CreatedDate" ON "KnowledgeRequests" ("CreatedDate");

-- ============================================================
-- NOTIFICATIONS
-- ============================================================
CREATE TABLE "Notifications" (
    "Id"                UUID            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          UUID            NOT NULL,
    "UserId"            UUID            NOT NULL,
    "NotificationType"  INT             NOT NULL,
    "Title"             VARCHAR(300)    NOT NULL,
    "Body"              VARCHAR(1000)   NOT NULL,
    "RelatedEntityType" VARCHAR(50),
    "RelatedEntityId"   UUID,
    "IsRead"            BOOLEAN         NOT NULL DEFAULT FALSE,
    "ReadAt"            TIMESTAMPTZ,
    "CreatedDate"       TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID            NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID            NOT NULL,
    "RecordVersion"     INT             NOT NULL DEFAULT 1,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Notifications_TenantId_UserId_IsRead" ON "Notifications" ("TenantId", "UserId", "IsRead");
CREATE INDEX "IX_Notifications_CreatedDate" ON "Notifications" ("CreatedDate");
