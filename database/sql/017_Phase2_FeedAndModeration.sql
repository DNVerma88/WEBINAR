-- ─────────────────────────────────────────────────────────────────
-- 017: Phase 2 — User Follows, Content Reports, Post View Events,
--               Full-Text Search vector on CommunityPosts
-- ─────────────────────────────────────────────────────────────────

-- ── User / Tag Follows ────────────────────────────────────────────
-- Note: UserTagFollows (tag-only) already exists from migration 016.
-- This migration adds the user-follow side.
ALTER TABLE "UserTagFollows"
    ADD COLUMN IF NOT EXISTS "FollowedUserId" UUID REFERENCES "Users" ("Id") ON DELETE CASCADE;

-- Partial unique index for user-to-user follow
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserTagFollows_Follower_User"
    ON "UserTagFollows" ("TenantId", "FollowerId", "FollowedUserId")
    WHERE "FollowedUserId" IS NOT NULL;

CREATE INDEX IF NOT EXISTS "IX_UserTagFollows_FollowedUserId"
    ON "UserTagFollows" ("FollowedUserId")
    WHERE "FollowedUserId" IS NOT NULL;

-- ── Content Reports ───────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "ContentReports" (
    "Id"              UUID        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"        UUID        NOT NULL,
    "ReporterId"      UUID        NOT NULL REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    "TargetPostId"    UUID        REFERENCES "CommunityPosts" ("Id") ON DELETE CASCADE,
    "TargetCommentId" UUID        REFERENCES "PostComments" ("Id") ON DELETE CASCADE,
    "ReasonCode"      SMALLINT    NOT NULL,
    "Description"     TEXT,
    "Status"          SMALLINT    NOT NULL DEFAULT 0,
    "ResolvedBy"      UUID        REFERENCES "Users" ("Id") ON DELETE SET NULL,
    "ResolvedAt"      TIMESTAMPTZ,
    "CreatedDate"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"       UUID        NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "ModifiedOn"      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"      UUID        NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "RecordVersion"   INT         NOT NULL DEFAULT 1,
    CONSTRAINT "PK_ContentReports" PRIMARY KEY ("Id"),
    CONSTRAINT "CHK_ContentReports_Target"
        CHECK ("TargetPostId" IS NOT NULL OR "TargetCommentId" IS NOT NULL)
);
CREATE INDEX IF NOT EXISTS "IX_ContentReports_TenantId_Status"
    ON "ContentReports" ("TenantId", "Status");

-- ── Post View Events (append-only analytics) ─────────────────────
CREATE TABLE IF NOT EXISTS "PostViewEvents" (
    "Id"         UUID        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"   UUID        NOT NULL,
    "PostId"     UUID        NOT NULL REFERENCES "CommunityPosts" ("Id") ON DELETE CASCADE,
    "ViewerId"   UUID        REFERENCES "Users" ("Id") ON DELETE SET NULL,
    "ViewedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "SessionKey" VARCHAR(64),
    CONSTRAINT "PK_PostViewEvents" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_PostViewEvents_PostId_ViewedAt"
    ON "PostViewEvents" ("PostId", "ViewedAt" DESC);

-- ── Full-Text Search on CommunityPosts ────────────────────────────
ALTER TABLE "CommunityPosts"
    ADD COLUMN IF NOT EXISTS "SearchVector" TSVECTOR
        GENERATED ALWAYS AS (
            to_tsvector('english', "Title" || ' ' || COALESCE("ContentMarkdown", ''))
        ) STORED;

CREATE INDEX IF NOT EXISTS "IX_CommunityPosts_SearchVector"
    ON "CommunityPosts" USING GIN ("SearchVector");

-- ── PostSeries — PostCount maintenance trigger ────────────────────
CREATE OR REPLACE FUNCTION update_series_post_count()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP = 'INSERT' AND NEW."SeriesId" IS NOT NULL THEN
        UPDATE "PostSeries" SET "PostCount" = "PostCount" + 1 WHERE "Id" = NEW."SeriesId";
    ELSIF TG_OP = 'UPDATE' THEN
        IF OLD."SeriesId" IS DISTINCT FROM NEW."SeriesId" THEN
            IF OLD."SeriesId" IS NOT NULL THEN
                UPDATE "PostSeries" SET "PostCount" = GREATEST(0, "PostCount" - 1) WHERE "Id" = OLD."SeriesId";
            END IF;
            IF NEW."SeriesId" IS NOT NULL THEN
                UPDATE "PostSeries" SET "PostCount" = "PostCount" + 1 WHERE "Id" = NEW."SeriesId";
            END IF;
        END IF;
    ELSIF TG_OP = 'DELETE' AND OLD."SeriesId" IS NOT NULL THEN
        UPDATE "PostSeries" SET "PostCount" = GREATEST(0, "PostCount" - 1) WHERE "Id" = OLD."SeriesId";
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_series_post_count ON "CommunityPosts";
CREATE TRIGGER trg_series_post_count
AFTER INSERT OR UPDATE OF "SeriesId" OR DELETE ON "CommunityPosts"
FOR EACH ROW EXECUTE FUNCTION update_series_post_count();

-- ── Tag PostCount maintenance trigger ─────────────────────────────
CREATE OR REPLACE FUNCTION update_tag_post_count()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE "Tags" SET "PostCount" = "PostCount" + 1 WHERE "Id" = NEW."TagId";
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE "Tags" SET "PostCount" = GREATEST(0, "PostCount" - 1) WHERE "Id" = OLD."TagId";
    END IF;
    RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS trg_tag_post_count ON "CommunityPostTags";
CREATE TRIGGER trg_tag_post_count
AFTER INSERT OR DELETE ON "CommunityPostTags"
FOR EACH ROW EXECUTE FUNCTION update_tag_post_count();
