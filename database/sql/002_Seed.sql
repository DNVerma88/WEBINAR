-- KnowHub Seed Data
-- Migration: 002_Seed.sql
-- Creates the SierraDev tenant and a SuperAdmin bootstrap user.
--
-- Credentials:
--   Tenant slug : sierradev
--   Email       : deep.narayan@sierradev.com
--   Password    : Deep@1234
--   Role        : SuperAdmin (32)

-- Use deterministic UUIDs so re-running is safe (ON CONFLICT DO NOTHING)
DO $$
DECLARE
    v_system_id UUID := '00000000-0000-0000-0000-000000000001';
    v_tenant_id UUID := '11111111-1111-1111-1111-111111111111';
    v_user_id   UUID := '22222222-2222-2222-2222-222222222222';
BEGIN

    -- -------------------------------------------------------
    -- Tenant: SierraDev
    -- -------------------------------------------------------
    INSERT INTO "Tenants" (
        "Id", "Name", "Slug", "IsActive",
        "CreatedDate", "CreatedBy", "ModifiedOn", "ModifiedBy", "RecordVersion"
    )
    VALUES (
        v_tenant_id, 'SierraDev', 'sierradev', TRUE,
        NOW(), v_system_id, NOW(), v_system_id, 1
    )
    ON CONFLICT ("Id") DO NOTHING;

    -- -------------------------------------------------------
    -- SuperAdmin User: Deep Narayan
    -- Password: Deep@1234  (BCrypt cost 12)
    -- Role = 32 (SuperAdmin)
    -- -------------------------------------------------------
    INSERT INTO "Users" (
        "Id", "TenantId", "FullName", "Email", "PasswordHash",
        "Department", "Designation", "Role", "IsActive",
        "CreatedDate", "CreatedBy", "ModifiedOn", "ModifiedBy", "RecordVersion"
    )
    VALUES (
        v_user_id, v_tenant_id,
        'Deep Narayan',
        'deep.narayan@sierradev.com',
        '$2a$12$Z1bYmdxNdlIjXLvmzGs4xOZdys3O3QZh51G/zhS4ErY4U0IvI3WO.',
        'Engineering', 'Super Admin',
        32, TRUE,
        NOW(), v_system_id, NOW(), v_system_id, 1
    )
    ON CONFLICT DO NOTHING;

    -- -------------------------------------------------------
    -- Starter Categories
    -- -------------------------------------------------------
    INSERT INTO "Categories" ("Id","TenantId","Name","Description","SortOrder","IsActive","CreatedDate","CreatedBy","ModifiedOn","ModifiedBy","RecordVersion")
    VALUES
        (gen_random_uuid(), v_tenant_id, 'Engineering',     'Software engineering topics',             1, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'Architecture',    'System and solution architecture',        2, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'DevOps',          'CI/CD, cloud, infrastructure',            3, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'Quality Assurance','Testing strategies and tools',           4, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'Product',         'Product management and strategy',         5, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'Data & AI',       'Data engineering, ML and AI topics',      6, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'Leadership',      'Leadership, management and soft skills',  7, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'Security',        'Cybersecurity and compliance',            8, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1)
    ON CONFLICT DO NOTHING;

    -- -------------------------------------------------------
    -- Starter Tags
    -- -------------------------------------------------------
    INSERT INTO "Tags" ("Id","TenantId","Name","Slug","UsageCount","IsActive","CreatedDate","CreatedBy","ModifiedOn","ModifiedBy","RecordVersion")
    VALUES
        (gen_random_uuid(), v_tenant_id, 'React',        'react',        0, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, '.NET',         'dotnet',       0, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'Kubernetes',   'kubernetes',   0, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'Docker',       'docker',       0, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'PostgreSQL',   'postgresql',   0, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'API Design',   'api-design',   0, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'TypeScript',   'typescript',   0, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'Azure',        'azure',        0, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'Testing',      'testing',      0, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1),
        (gen_random_uuid(), v_tenant_id, 'Clean Architecture', 'clean-architecture', 0, TRUE, NOW(), v_user_id, NOW(), v_user_id, 1)
    ON CONFLICT DO NOTHING;

END $$;
