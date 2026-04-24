-- ============================================================
-- Migration 016: Dev Community Enhancement — Core Posts & Engagement
-- ============================================================

-- ─────────────────────────────────────────────────────────────────
-- Extend Tags table with community-post specific columns
-- ─────────────────────────────────────────────────────────────────
ALTER TABLE "Tags"
    ADD COLUMN IF NOT EXISTS "Description"  TEXT,
    ADD COLUMN IF NOT EXISTS "PostCount"    INT  NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "IsOfficial"   BOOL NOT NULL DEFAULT FALSE;

-- ─────────────────────────────────────────────────────────────────
-- PostSeries
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "PostSeries" (
    "Id"            UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID          NOT NULL,
    "CommunityId"   UUID          NOT NULL,
    "AuthorId"      UUID          NOT NULL,
    "Title"         VARCHAR(300)  NOT NULL,
    "Slug"          VARCHAR(300)  NOT NULL,
    "Description"   TEXT,
    "PostCount"     INT           NOT NULL DEFAULT 0,
    "CreatedDate"   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID          NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID          NOT NULL,
    "RecordVersion" INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_PostSeries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PostSeries_Communities" FOREIGN KEY ("CommunityId")
        REFERENCES "Communities" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PostSeries_Users_Author" FOREIGN KEY ("AuthorId")
        REFERENCES "Users" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_PostSeries_TenantId_CommunityId_Slug"
    ON "PostSeries" ("TenantId", "CommunityId", "Slug");

-- ─────────────────────────────────────────────────────────────────
-- CommunityPosts
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "CommunityPosts" (
    "Id"                  UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"            UUID          NOT NULL,
    "CommunityId"         UUID          NOT NULL,
    "AuthorId"            UUID          NOT NULL,
    "SeriesId"            UUID,
    "SeriesOrder"         INT,
    "Title"               VARCHAR(300)  NOT NULL,
    "Slug"                VARCHAR(300)  NOT NULL,
    "ContentMarkdown"     TEXT          NOT NULL,
    "ContentHtml"         TEXT          NOT NULL,
    "CoverImageUrl"       VARCHAR(500),
    "CanonicalUrl"        VARCHAR(1000),
    "PostType"            SMALLINT      NOT NULL DEFAULT 0,
    "Status"              SMALLINT      NOT NULL DEFAULT 0,
    "ReadingTimeMinutes"  INT           NOT NULL DEFAULT 1,
    "ReactionCount"       INT           NOT NULL DEFAULT 0,
    "CommentCount"        INT           NOT NULL DEFAULT 0,
    "ViewCount"           BIGINT        NOT NULL DEFAULT 0,
    "BookmarkCount"       INT           NOT NULL DEFAULT 0,
    "PublishedAt"         TIMESTAMPTZ,
    "ScheduledAt"         TIMESTAMPTZ,
    "IsFeatured"          BOOLEAN       NOT NULL DEFAULT FALSE,
    "LastDraftSavedAt"    TIMESTAMPTZ,
    "CreatedDate"         TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"           UUID          NOT NULL,
    "ModifiedOn"          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"          UUID          NOT NULL,
    "RecordVersion"       INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_CommunityPosts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommunityPosts_Communities" FOREIGN KEY ("CommunityId")
        REFERENCES "Communities" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CommunityPosts_Users_Author" FOREIGN KEY ("AuthorId")
        REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CommunityPosts_PostSeries" FOREIGN KEY ("SeriesId")
        REFERENCES "PostSeries" ("Id") ON DELETE SET NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_CommunityPosts_TenantId_CommunityId_Slug"
    ON "CommunityPosts" ("TenantId", "CommunityId", "Slug");
CREATE INDEX IF NOT EXISTS "IX_CommunityPosts_TenantId_CommunityId_Status_PublishedAt"
    ON "CommunityPosts" ("TenantId", "CommunityId", "Status", "PublishedAt" DESC);
CREATE INDEX IF NOT EXISTS "IX_CommunityPosts_TenantId_AuthorId"
    ON "CommunityPosts" ("TenantId", "AuthorId");

-- Full-text search vector
ALTER TABLE "CommunityPosts"
    ADD COLUMN IF NOT EXISTS "SearchVector" TSVECTOR
        GENERATED ALWAYS AS (
            to_tsvector('english', "Title" || ' ' || coalesce("ContentMarkdown", ''))
        ) STORED;
CREATE INDEX IF NOT EXISTS "IX_CommunityPosts_SearchVector"
    ON "CommunityPosts" USING GIN ("SearchVector");

-- ─────────────────────────────────────────────────────────────────
-- CommunityPostTags  (M:N join)
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "CommunityPostTags" (
    "PostId"    UUID NOT NULL,
    "TagId"     UUID NOT NULL,
    "TenantId"  UUID NOT NULL,
    CONSTRAINT "PK_CommunityPostTags" PRIMARY KEY ("PostId", "TagId"),
    CONSTRAINT "FK_CommunityPostTags_Post" FOREIGN KEY ("PostId")
        REFERENCES "CommunityPosts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CommunityPostTags_Tag" FOREIGN KEY ("TagId")
        REFERENCES "Tags" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_CommunityPostTags_TagId" ON "CommunityPostTags" ("TagId");

-- ─────────────────────────────────────────────────────────────────
-- PostReactions
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "PostReactions" (
    "Id"            UUID        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID        NOT NULL,
    "PostId"        UUID        NOT NULL,
    "UserId"        UUID        NOT NULL,
    "ReactionType"  SMALLINT    NOT NULL DEFAULT 0,
    "CreatedDate"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID        NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "ModifiedOn"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID        NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "RecordVersion" INT         NOT NULL DEFAULT 1,
    CONSTRAINT "PK_PostReactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PostReactions_Post" FOREIGN KEY ("PostId")
        REFERENCES "CommunityPosts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_PostReactions_User_Post_Type"
        UNIQUE ("TenantId", "PostId", "UserId", "ReactionType")
);
CREATE INDEX IF NOT EXISTS "IX_PostReactions_PostId" ON "PostReactions" ("PostId");

-- ─────────────────────────────────────────────────────────────────
-- PostComments
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "PostComments" (
    "Id"              UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"        UUID          NOT NULL,
    "PostId"          UUID          NOT NULL,
    "AuthorId"        UUID          NOT NULL,
    "ParentCommentId" UUID,
    "BodyMarkdown"    TEXT          NOT NULL,
    "IsDeleted"       BOOLEAN       NOT NULL DEFAULT FALSE,
    "LikeCount"       INT           NOT NULL DEFAULT 0,
    "CreatedDate"     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"       UUID          NOT NULL,
    "ModifiedOn"      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"      UUID          NOT NULL,
    "RecordVersion"   INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_PostComments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PostComments_Post" FOREIGN KEY ("PostId")
        REFERENCES "CommunityPosts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PostComments_Parent" FOREIGN KEY ("ParentCommentId")
        REFERENCES "PostComments" ("Id") ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS "IX_PostComments_PostId_CreatedDate"
    ON "PostComments" ("PostId", "CreatedDate");

-- ─────────────────────────────────────────────────────────────────
-- PostBookmarks
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "PostBookmarks" (
    "Id"            UUID        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID        NOT NULL,
    "UserId"        UUID        NOT NULL,
    "PostId"        UUID        NOT NULL,
    "BookmarkedAt"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedDate"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID        NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID        NOT NULL,
    "RecordVersion" INT         NOT NULL DEFAULT 1,
    CONSTRAINT "PK_PostBookmarks" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_PostBookmarks_User_Post"
        UNIQUE ("TenantId", "UserId", "PostId"),
    CONSTRAINT "FK_PostBookmarks_Post" FOREIGN KEY ("PostId")
        REFERENCES "CommunityPosts" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_PostBookmarks_UserId" ON "PostBookmarks" ("UserId");

-- ─────────────────────────────────────────────────────────────────
-- UserTagFollows  (follow users OR tags for feed personalization)
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "UserTagFollows" (
    "Id"             UUID        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"       UUID        NOT NULL,
    "FollowerId"     UUID        NOT NULL,
    "FollowedUserId" UUID,
    "FollowedTagId"  UUID,
    "FollowedAt"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedDate"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"      UUID        NOT NULL,
    "ModifiedOn"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"     UUID        NOT NULL,
    "RecordVersion"  INT         NOT NULL DEFAULT 1,
    CONSTRAINT "PK_UserTagFollows" PRIMARY KEY ("Id"),
    CONSTRAINT "CHK_UserTagFollows_Target"
        CHECK ("FollowedUserId" IS NOT NULL OR "FollowedTagId" IS NOT NULL),
    CONSTRAINT "FK_UserTagFollows_Follower" FOREIGN KEY ("FollowerId")
        REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserTagFollows_FollowedUser" FOREIGN KEY ("FollowedUserId")
        REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserTagFollows_FollowedTag" FOREIGN KEY ("FollowedTagId")
        REFERENCES "Tags" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_UserTagFollows_FollowerId" ON "UserTagFollows" ("FollowerId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserTagFollows_User_Follow"
    ON "UserTagFollows" ("TenantId", "FollowerId", "FollowedUserId")
    WHERE "FollowedUserId" IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserTagFollows_Tag_Follow"
    ON "UserTagFollows" ("TenantId", "FollowerId", "FollowedTagId")
    WHERE "FollowedTagId" IS NOT NULL;
