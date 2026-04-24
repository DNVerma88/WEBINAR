-- migration
-- ============================================================
-- Migration 011: Survey Module
-- ============================================================

-- ─── Enum Types (idempotent) ────────────────────────────────

DO $$ BEGIN
    CREATE TYPE "SurveyStatus" AS ENUM ('Draft','Active','Closed');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE "SurveyQuestionType" AS ENUM (
        'Text','SingleChoice','MultipleChoice','Rating','YesNo'
    );
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE "SurveyInvitationStatus" AS ENUM (
        'Pending','Sent','Submitted','Expired','Failed'
    );
EXCEPTION WHEN duplicate_object THEN null; END $$;

-- ─── Surveys ────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS "Surveys" (
    "Id"                UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"          UUID        NOT NULL,
    "Title"             VARCHAR(300) NOT NULL,
    "Description"       TEXT,
    "WelcomeMessage"    TEXT,
    "ThankYouMessage"   TEXT,
    "Status"            VARCHAR(20) NOT NULL DEFAULT 'Draft',
    "TokenExpiryDays"   INT         NOT NULL DEFAULT 7,
    "IsAnonymous"       BOOLEAN     NOT NULL DEFAULT FALSE,
    "LaunchedAt"        TIMESTAMPTZ,
    "ClosedAt"          TIMESTAMPTZ,
    "TotalInvited"      INT         NOT NULL DEFAULT 0,
    "TotalResponded"    INT         NOT NULL DEFAULT 0,
    "CreatedDate"       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID        NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID        NOT NULL,
    "RecordVersion"     INT         NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS "IX_Surveys_TenantId"
    ON "Surveys"("TenantId");

CREATE INDEX IF NOT EXISTS "IX_Surveys_TenantId_Status"
    ON "Surveys"("TenantId", "Status");

-- ─── SurveyQuestions ────────────────────────────────────────

CREATE TABLE IF NOT EXISTS "SurveyQuestions" (
    "Id"              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"        UUID        NOT NULL,
    "SurveyId"        UUID        NOT NULL REFERENCES "Surveys"("Id") ON DELETE CASCADE,
    "QuestionText"    TEXT        NOT NULL,
    "QuestionType"    VARCHAR(30) NOT NULL,
    "OptionsJson"     JSONB,
    "MinRating"       INT         NOT NULL DEFAULT 1,
    "MaxRating"       INT         NOT NULL DEFAULT 5,
    "IsRequired"      BOOLEAN     NOT NULL DEFAULT TRUE,
    "OrderSequence"   INT         NOT NULL DEFAULT 0,
    "CreatedDate"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"       UUID        NOT NULL,
    "ModifiedOn"      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"      UUID        NOT NULL,
    "RecordVersion"   INT         NOT NULL DEFAULT 1,
    CONSTRAINT "CHK_SurveyQuestions_RatingRange"
        CHECK (
            "QuestionType" <> 'Rating'
            OR ("MinRating" >= 0 AND "MaxRating" <= 10 AND "MinRating" < "MaxRating")
        )
);

CREATE INDEX IF NOT EXISTS "IX_SurveyQuestions_TenantId"
    ON "SurveyQuestions"("TenantId");

CREATE INDEX IF NOT EXISTS "IX_SurveyQuestions_SurveyId_Order"
    ON "SurveyQuestions"("SurveyId", "OrderSequence");

-- ─── SurveyInvitations ──────────────────────────────────────

CREATE TABLE IF NOT EXISTS "SurveyInvitations" (
    "Id"               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"         UUID        NOT NULL,
    "SurveyId"         UUID        NOT NULL REFERENCES "Surveys"("Id") ON DELETE CASCADE,
    "UserId"           UUID        NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "TokenHash"        CHAR(64)    NOT NULL,
    "Status"           VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "SentAt"           TIMESTAMPTZ,
    "ExpiresAt"        TIMESTAMPTZ,
    "SubmittedAt"      TIMESTAMPTZ,
    "TokenAccessedAt"  TIMESTAMPTZ,
    "ResendCount"      INT         NOT NULL DEFAULT 0,
    "CreatedDate"      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"        UUID        NOT NULL,
    "ModifiedOn"       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"       UUID        NOT NULL,
    "RecordVersion"    INT         NOT NULL DEFAULT 1,
    CONSTRAINT "UQ_SurveyInvitations_SurveyId_UserId"
        UNIQUE ("TenantId", "SurveyId", "UserId"),
    CONSTRAINT "UQ_SurveyInvitations_TokenHash"
        UNIQUE ("TokenHash")
);

CREATE INDEX IF NOT EXISTS "IX_SurveyInvitations_TenantId"
    ON "SurveyInvitations"("TenantId");

CREATE INDEX IF NOT EXISTS "IX_SurveyInvitations_SurveyId_Status"
    ON "SurveyInvitations"("SurveyId", "Status");

CREATE INDEX IF NOT EXISTS "IX_SurveyInvitations_TokenHash"
    ON "SurveyInvitations"("TokenHash");

CREATE INDEX IF NOT EXISTS "IX_SurveyInvitations_UserId"
    ON "SurveyInvitations"("UserId");

-- ─── SurveyResponses ────────────────────────────────────────

CREATE TABLE IF NOT EXISTS "SurveyResponses" (
    "Id"            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"      UUID        NOT NULL,
    "SurveyId"      UUID        NOT NULL REFERENCES "Surveys"("Id") ON DELETE RESTRICT,
    "UserId"        UUID        NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "InvitationId"  UUID        NOT NULL REFERENCES "SurveyInvitations"("Id") ON DELETE RESTRICT,
    "SubmittedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedDate"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID        NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID        NOT NULL,
    "RecordVersion" INT         NOT NULL DEFAULT 1,
    CONSTRAINT "UQ_SurveyResponses_SurveyId_UserId"
        UNIQUE ("TenantId", "SurveyId", "UserId")
);

CREATE INDEX IF NOT EXISTS "IX_SurveyResponses_TenantId"
    ON "SurveyResponses"("TenantId");

CREATE INDEX IF NOT EXISTS "IX_SurveyResponses_SurveyId"
    ON "SurveyResponses"("SurveyId");

-- ─── SurveyAnswers ──────────────────────────────────────────

CREATE TABLE IF NOT EXISTS "SurveyAnswers" (
    "Id"                UUID    PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"          UUID    NOT NULL,
    "ResponseId"        UUID    NOT NULL REFERENCES "SurveyResponses"("Id") ON DELETE CASCADE,
    "QuestionId"        UUID    NOT NULL REFERENCES "SurveyQuestions"("Id") ON DELETE RESTRICT,
    "AnswerText"        TEXT,
    "AnswerOptionsJson" JSONB,
    "RatingValue"       INT,
    "CreatedDate"       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID    NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID    NOT NULL,
    "RecordVersion"     INT     NOT NULL DEFAULT 1,
    CONSTRAINT "UQ_SurveyAnswers_ResponseId_QuestionId"
        UNIQUE ("ResponseId", "QuestionId")
);

CREATE INDEX IF NOT EXISTS "IX_SurveyAnswers_TenantId"
    ON "SurveyAnswers"("TenantId");

CREATE INDEX IF NOT EXISTS "IX_SurveyAnswers_ResponseId"
    ON "SurveyAnswers"("ResponseId");

CREATE INDEX IF NOT EXISTS "IX_SurveyAnswers_QuestionId"
    ON "SurveyAnswers"("QuestionId");
