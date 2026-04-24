-- Migration 007: Add WorkRoles master table and link to AIAssessmentGroupEmployees

CREATE TABLE IF NOT EXISTS "WorkRoles" (
    "Id"            UUID         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "TenantId"      UUID         NOT NULL,
    "Code"          VARCHAR(50)  NOT NULL,
    "Name"          VARCHAR(150) NOT NULL,
    "Category"      VARCHAR(100) NOT NULL DEFAULT '',
    "DisplayOrder"  INT          NOT NULL DEFAULT 0,
    "IsActive"      BOOL         NOT NULL DEFAULT TRUE,
    "CreatedDate"   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "ModifiedOn"    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID         NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "ModifiedBy"    UUID         NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "RecordVersion" INT          NOT NULL DEFAULT 1,
    CONSTRAINT "UQ_WorkRoles_TenantId_Code" UNIQUE ("TenantId", "Code")
);

ALTER TABLE "AIAssessmentGroupEmployees"
    ADD COLUMN IF NOT EXISTS "WorkRoleId" UUID
        REFERENCES "WorkRoles"("Id") ON DELETE SET NULL;

-- Seed common work roles for the default tenant
-- (Uses the tenant from existing seed data)
INSERT INTO "WorkRoles" ("Id", "TenantId", "Code", "Name", "Category", "DisplayOrder", "IsActive", "CreatedBy", "ModifiedBy")
SELECT
    gen_random_uuid(),
    t."Id",
    r."Code",
    r."Name",
    r."Category",
    r."DisplayOrder",
    TRUE,
    '00000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000001'
FROM "Tenants" t
CROSS JOIN (VALUES
    ('DEV-BE',   'Developer - Backend',        'Engineering', 10),
    ('DEV-FE',   'Developer - Frontend',       'Engineering', 20),
    ('DEV-FS',   'Developer - Full Stack',     'Engineering', 30),
    ('QA-MANUAL','QA - Manual',                'Quality',     40),
    ('QA-AUTO',  'QA - Automation',            'Quality',     50),
    ('DEVOPS',   'DevOps / Cloud Engineer',    'Engineering', 60),
    ('DM',       'Delivery Manager',           'Management',  70),
    ('PM',       'Project Manager',            'Management',  80),
    ('BA',       'Business Analyst / Product', 'Product',     90),
    ('DESIGN',   'UI/UX Designer',             'Product',    100),
    ('SCRUM-M',  'Scrum Master',               'Management', 110),
    ('ARCH',     'Solution Architect',         'Engineering',120)
) AS r("Code", "Name", "Category", "DisplayOrder")
WHERE NOT EXISTS (
    SELECT 1 FROM "WorkRoles" wr WHERE wr."TenantId" = t."Id" AND wr."Code" = r."Code"
);
