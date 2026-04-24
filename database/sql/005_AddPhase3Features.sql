-- ============================================================
-- Migration 005: Phase 3 – Moderation, Peer Review, Speaker
--                Marketplace, Email Logs
-- ============================================================

-- ContentFlags
CREATE TABLE "ContentFlags" (
    "Id"               UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"         UUID         NOT NULL REFERENCES "Tenants"("Id"),
    "FlaggedByUserId"  UUID         NOT NULL REFERENCES "Users"("Id"),
    "ContentType"      VARCHAR(50)  NOT NULL,
    "ContentId"        UUID         NOT NULL,
    "Reason"           VARCHAR(50)  NOT NULL,
    "Notes"            TEXT,
    "Status"           VARCHAR(50)  NOT NULL DEFAULT 'Pending',
    "ReviewedByUserId" UUID         REFERENCES "Users"("Id"),
    "ReviewedAt"       TIMESTAMPTZ,
    "ReviewNotes"      TEXT,
    "CreatedDate"      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "CreatedBy"        UUID         NOT NULL,
    "ModifiedOn"       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "ModifiedBy"       UUID         NOT NULL,
    "RecordVersion"    INT          NOT NULL DEFAULT 1
);
CREATE INDEX "IX_ContentFlags_TenantId"        ON "ContentFlags"("TenantId");
CREATE INDEX "IX_ContentFlags_TenantId_Status" ON "ContentFlags"("TenantId", "Status");
CREATE INDEX "IX_ContentFlags_FlaggedByUserId" ON "ContentFlags"("FlaggedByUserId");
CREATE INDEX "IX_ContentFlags_ContentType_ContentId" ON "ContentFlags"("ContentType", "ContentId");

-- UserSuspensions
CREATE TABLE "UserSuspensions" (
    "Id"                UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"          UUID         NOT NULL REFERENCES "Tenants"("Id"),
    "UserId"            UUID         NOT NULL REFERENCES "Users"("Id"),
    "SuspendedByUserId" UUID         NOT NULL REFERENCES "Users"("Id"),
    "Reason"            TEXT         NOT NULL,
    "SuspendedAt"       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "ExpiresAt"         TIMESTAMPTZ,
    "IsActive"          BOOLEAN      NOT NULL DEFAULT TRUE,
    "LiftedByUserId"    UUID         REFERENCES "Users"("Id"),
    "LiftedAt"          TIMESTAMPTZ,
    "LiftReason"        TEXT,
    "CreatedDate"       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID         NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID         NOT NULL,
    "RecordVersion"     INT          NOT NULL DEFAULT 1
);
CREATE INDEX "IX_UserSuspensions_TenantId"        ON "UserSuspensions"("TenantId");
CREATE INDEX "IX_UserSuspensions_TenantId_UserId" ON "UserSuspensions"("TenantId", "UserId");

-- KnowledgeAssetReviews (Peer Review)
CREATE TABLE "KnowledgeAssetReviews" (
    "Id"                  UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"            UUID         NOT NULL REFERENCES "Tenants"("Id"),
    "KnowledgeAssetId"    UUID         NOT NULL REFERENCES "KnowledgeAssets"("Id"),
    "ReviewerId"          UUID         NOT NULL REFERENCES "Users"("Id"),
    "NominatedByUserId"   UUID         NOT NULL REFERENCES "Users"("Id"),
    "NominatedAt"         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "Status"              VARCHAR(50)  NOT NULL DEFAULT 'Pending',
    "Comments"            TEXT,
    "ReviewedAt"          TIMESTAMPTZ,
    "CreatedDate"         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "CreatedBy"           UUID         NOT NULL,
    "ModifiedOn"          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "ModifiedBy"          UUID         NOT NULL,
    "RecordVersion"       INT          NOT NULL DEFAULT 1
);
CREATE INDEX "IX_KnowledgeAssetReviews_TenantId"                     ON "KnowledgeAssetReviews"("TenantId");
CREATE INDEX "IX_KnowledgeAssetReviews_TenantId_KnowledgeAssetId"    ON "KnowledgeAssetReviews"("TenantId", "KnowledgeAssetId");
CREATE INDEX "IX_KnowledgeAssetReviews_ReviewerId"                   ON "KnowledgeAssetReviews"("ReviewerId");
CREATE INDEX "IX_KnowledgeAssetReviews_Status"                       ON "KnowledgeAssetReviews"("Status");

-- SpeakerAvailability
CREATE TABLE "SpeakerAvailability" (
    "Id"                 UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"           UUID         NOT NULL REFERENCES "Tenants"("Id"),
    "UserId"             UUID         NOT NULL REFERENCES "Users"("Id"),
    "AvailableFrom"      TIMESTAMPTZ  NOT NULL,
    "AvailableTo"        TIMESTAMPTZ  NOT NULL,
    "IsRecurring"        BOOLEAN      NOT NULL DEFAULT FALSE,
    "RecurrencePattern"  VARCHAR(50),
    "Topics"             JSONB        NOT NULL DEFAULT '[]',
    "Notes"              TEXT,
    "IsBooked"           BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedDate"        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "CreatedBy"          UUID         NOT NULL,
    "ModifiedOn"         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "ModifiedBy"         UUID         NOT NULL,
    "RecordVersion"      INT          NOT NULL DEFAULT 1
);
CREATE INDEX "IX_SpeakerAvailability_TenantId"        ON "SpeakerAvailability"("TenantId");
CREATE INDEX "IX_SpeakerAvailability_TenantId_UserId" ON "SpeakerAvailability"("TenantId", "UserId");
CREATE INDEX "IX_SpeakerAvailability_AvailableFrom"   ON "SpeakerAvailability"("AvailableFrom");

-- SpeakerBookings
CREATE TABLE "SpeakerBookings" (
    "Id"                      UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"                UUID         NOT NULL REFERENCES "Tenants"("Id"),
    "SpeakerAvailabilityId"   UUID         NOT NULL REFERENCES "SpeakerAvailability"("Id"),
    "SpeakerUserId"           UUID         NOT NULL REFERENCES "Users"("Id"),
    "RequesterUserId"         UUID         NOT NULL REFERENCES "Users"("Id"),
    "Topic"                   VARCHAR(500) NOT NULL,
    "Description"             TEXT,
    "Status"                  VARCHAR(50)  NOT NULL DEFAULT 'Pending',
    "RespondedAt"             TIMESTAMPTZ,
    "ResponseNotes"           TEXT,
    "LinkedSessionId"         UUID         REFERENCES "Sessions"("Id"),
    "CreatedDate"             TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "CreatedBy"               UUID         NOT NULL,
    "ModifiedOn"              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "ModifiedBy"              UUID         NOT NULL,
    "RecordVersion"           INT          NOT NULL DEFAULT 1
);
CREATE INDEX "IX_SpeakerBookings_TenantId"                  ON "SpeakerBookings"("TenantId");
CREATE INDEX "IX_SpeakerBookings_TenantId_SpeakerUserId"    ON "SpeakerBookings"("TenantId", "SpeakerUserId");
CREATE INDEX "IX_SpeakerBookings_TenantId_RequesterUserId"  ON "SpeakerBookings"("TenantId", "RequesterUserId");

-- EmailLogs
CREATE TABLE "EmailLogs" (
    "Id"               UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"         UUID         NOT NULL REFERENCES "Tenants"("Id"),
    "RecipientEmail"   VARCHAR(255) NOT NULL,
    "RecipientUserId"  UUID         REFERENCES "Users"("Id"),
    "Subject"          VARCHAR(500) NOT NULL,
    "EmailType"        VARCHAR(100) NOT NULL,
    "Status"           VARCHAR(50)  NOT NULL,
    "SentAt"           TIMESTAMPTZ,
    "ErrorMessage"     TEXT,
    "MessageId"        VARCHAR(500),
    "CreatedDate"      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "CreatedBy"        UUID         NOT NULL,
    "ModifiedOn"       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "ModifiedBy"       UUID         NOT NULL,
    "RecordVersion"    INT          NOT NULL DEFAULT 1
);
CREATE INDEX "IX_EmailLogs_TenantId"          ON "EmailLogs"("TenantId");
CREATE INDEX "IX_EmailLogs_RecipientUserId"   ON "EmailLogs"("RecipientUserId");
CREATE INDEX "IX_EmailLogs_SentAt"            ON "EmailLogs"("SentAt");
CREATE INDEX "IX_EmailLogs_EmailType"         ON "EmailLogs"("EmailType");
