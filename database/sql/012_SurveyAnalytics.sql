-- migration
-- ============================================================
-- Migration 012: Survey Analytics
-- ============================================================

-- Add TokenAccessedAt for participation funnel analytics
ALTER TABLE "SurveyInvitations"
    ADD COLUMN IF NOT EXISTS "TokenAccessedAt" TIMESTAMPTZ NULL;

-- Lightweight index additions for analytics join performance
CREATE INDEX IF NOT EXISTS "IX_SurveyAnswers_QuestionId_RatingValue"
    ON "SurveyAnswers" ("QuestionId", "RatingValue")
    WHERE "RatingValue" IS NOT NULL;

-- Materialized view for per-question option statistics
-- (refreshed by SurveyAnalyticsService after cache eviction)
CREATE MATERIALIZED VIEW IF NOT EXISTS "mv_SurveyOptionStats" AS
SELECT
    sq."Id"            AS "QuestionId",
    sq."SurveyId",
    sq."QuestionType",
    sa."AnswerText"    AS "OptionValue",
    COUNT(*)           AS "AnswerCount"
FROM "SurveyAnswers" sa
JOIN "SurveyQuestions" sq ON sq."Id" = sa."QuestionId"
WHERE sa."AnswerText" IS NOT NULL
GROUP BY sq."Id", sq."SurveyId", sq."QuestionType", sa."AnswerText";

CREATE UNIQUE INDEX IF NOT EXISTS "idx_mv_SurveyOptionStats"
    ON "mv_SurveyOptionStats" ("QuestionId", "OptionValue");
