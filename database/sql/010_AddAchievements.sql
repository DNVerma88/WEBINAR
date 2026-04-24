-- Migration 010: Add Achievements column to ResumeProfiles
-- Adds a JSONB column for storing achievement entries (title, year, description)

ALTER TABLE "ResumeProfiles"
    ADD COLUMN IF NOT EXISTS "Achievements" JSONB NOT NULL DEFAULT '[]';
