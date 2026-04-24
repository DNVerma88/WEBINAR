using KnowHub.Domain.Enums;
using KnowHub.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Tests.TestHelpers;

public static class TestDbFactory
{
    public static KnowHubDbContext CreateInMemory(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<KnowHubDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new KnowHubDbContext(options);
    }

    /// <summary>
    /// Creates an SQLite in-memory DbContext. Required for tests that use
    /// ExecuteUpdateAsync / ExecuteDeleteAsync which are not supported by EF InMemory.
    /// The caller is responsible for disposing the connection.
    /// </summary>
    public static (KnowHubDbContext db, SqliteConnection connection) CreateSqliteInMemory()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<KnowHubDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new KnowHubDbContext(options);
        db.Database.EnsureCreated();
        return (db, connection);
    }

    public static async Task<(KnowHubDbContext db, SqliteConnection connection, Guid tenantId, Guid userId)>
        CreateWithSeedSqliteAsync(UserRole role = UserRole.Employee)
    {
        var (db, conn) = CreateSqliteInMemory();
        var tenantId = Guid.NewGuid();
        var userId   = Guid.NewGuid();

        var tenant = new Domain.Entities.Tenant
        {
            Id        = tenantId,
            Name      = "Test Tenant",
            Slug      = "test-tenant",
            IsActive  = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        db.Tenants.Add(tenant);

        var user = new Domain.Entities.User
        {
            Id           = userId,
            TenantId     = tenantId,
            FullName     = "Test User",
            Email        = "test@knowhub.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role         = role,
            IsActive     = true,
            CreatedBy    = userId,
            ModifiedBy   = userId
        };
        db.Users.Add(user);

        await db.SaveChangesAsync();
        return (db, conn, tenantId, userId);
    }

    public static async Task<(KnowHubDbContext db, Guid tenantId, Guid userId)> CreateWithSeedAsync(
        UserRole role = UserRole.Employee,
        string? dbName = null)
    {
        var db = CreateInMemory(dbName);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var tenant = new Domain.Entities.Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
            IsActive = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        db.Tenants.Add(tenant);

        var user = new Domain.Entities.User
        {
            Id = userId,
            TenantId = tenantId,
            FullName = "Test User",
            Email = "test@knowhub.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = role,
            IsActive = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        db.Users.Add(user);

        await db.SaveChangesAsync();
        return (db, tenantId, userId);
    }
}
