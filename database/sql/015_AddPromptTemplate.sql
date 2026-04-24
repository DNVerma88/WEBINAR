-- 015_AddPromptTemplate.sql
-- Adds a per-job AI scoring prompt template column to ScreeningJobs.
-- NULL means: use ResumeScorer.DefaultPromptTemplate (the built-in expert evaluator prompt).

ALTER TABLE "ScreeningJobs"
    ADD COLUMN IF NOT EXISTS "PromptTemplate" text;
