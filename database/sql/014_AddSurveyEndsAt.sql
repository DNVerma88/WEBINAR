-- ============================================================
-- Migration 014: Add EndsAt to Surveys
-- Replaces the TokenExpiryDays-based approach with a concrete
-- end date that drives invitation token expiry directly.
-- ============================================================

ALTER TABLE "Surveys"
    ADD COLUMN IF NOT EXISTS "EndsAt" TIMESTAMPTZ;
