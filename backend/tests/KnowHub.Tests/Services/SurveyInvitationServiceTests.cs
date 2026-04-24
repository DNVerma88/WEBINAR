using System.Security.Cryptography;
using System.Text;
using KnowHub.Application.Contracts.Email;
using KnowHub.Application.Models.Surveys;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services.Surveys;
using KnowHub.Tests.TestHelpers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowHub.Tests.Services;

public class SurveyInvitationServiceTests
{
    // -- Helpers ------------------------------------------------------------

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Survey:FrontendBaseUrl"] = "http://localhost:5173"
            })
            .Build();

    private static SurveyInvitationService CreateService(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        KnowHub.Application.Contracts.ICurrentUserAccessor currentUser,
        FakeEmailService? emailService = null)
    {
        return new SurveyInvitationService(
            db,
            currentUser,
            emailService ?? new FakeEmailService(),
            BuildConfig(),
            NullLogger<SurveyInvitationService>.Instance);
    }

    private static string HashToken(string plainToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<Survey> SeedActiveSurveyAsync(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        Guid tenantId, Guid userId, SurveyStatus status = SurveyStatus.Active)
    {
        var survey = new Survey
        {
            TenantId        = tenantId,
            Title           = "Team Health Survey",
            Status          = status,
            TokenExpiryDays = 7,
            LaunchedAt      = DateTime.UtcNow.AddHours(-1),
            CreatedBy       = userId,
            ModifiedBy      = userId,
        };
        db.Surveys.Add(survey);
        await db.SaveChangesAsync();
        return survey;
    }

    private static async Task<SurveyInvitation> SeedInvitationAsync(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        Guid tenantId, Guid userId, Guid surveyId,
        SurveyInvitationStatus status = SurveyInvitationStatus.Sent,
        DateTime? expiresAt = null)
    {
        var bytes      = RandomNumberGenerator.GetBytes(32);
        var plainToken = Base64UrlTextEncoder.Encode(bytes);
        var tokenHash  = HashToken(plainToken);

        var invitation = new SurveyInvitation
        {
            TenantId  = tenantId,
            SurveyId  = surveyId,
            UserId    = userId,
            TokenHash = tokenHash,
            Status    = status,
            SentAt    = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(6),
            CreatedBy = userId,
            ModifiedBy = userId,
        };
        db.SurveyInvitations.Add(invitation);
        await db.SaveChangesAsync();
        return invitation;
    }

    // -- Token generation --------------------------------------------------

    [Fact]
    public async Task GenerateToken_ProducesUnique_NonStoredPlaintext()
    {
        // Use CreateInvitationsAndSendAsync to trigger real token generation,
        // then verify the stored TokenHash is NOT the plaintext itself but IS 64-char hex.
        var (db, conn, tenantId, userId) = await TestDbFactory.CreateWithSeedSqliteAsync(UserRole.Employee);
        using var _ = conn;
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId, TenantId = tenantId, FullName = "Admin User",
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234!"),
            Role = UserRole.Admin, IsActive = true,
            CreatedBy = userId, ModifiedBy = userId,
        };
        db.Users.Add(admin);

        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var emailSvc = new FakeEmailService();
        var sut = CreateService(db, currentUser, emailSvc);
        var survey = await SeedActiveSurveyAsync(db, tenantId, userId);
        await db.SaveChangesAsync();

        await sut.CreateInvitationsAndSendAsync(survey.Id, CancellationToken.None);

        var invitation = db.SurveyInvitations.First();

        // TokenHash must be exactly 64 lowercase hex characters
        Assert.Equal(64, invitation.TokenHash.Length);
        Assert.Matches("^[0-9a-f]{64}$", invitation.TokenHash);

        // Email was sent
        Assert.Single(emailSvc.SentEmails);
        var email = emailSvc.LastEmailOf<SurveyInvitationEmailData>();
        Assert.NotNull(email);

        // The URL in the email contains a plain token (NOT the hash) — it is shorter than 64 chars hex
        var tokenInUrl = email!.SurveyUrl.Split('/').Last();
        // Plain token (Base64Url ~43 chars) is not the same as the hash (64 chars hex)
        Assert.NotEqual(invitation.TokenHash, tokenInUrl);

        // Re-hashing the token in the URL must produce the stored hash
        var recomputed = HashToken(tokenInUrl);
        Assert.Equal(invitation.TokenHash, recomputed);
    }

    // -- CreateInvitationsAndSendAsync -------------------------------------

    [Fact]
    public async Task CreateInvitationsAsync_CorrectCount_Matching_ActiveEmployees()
    {
        var (db, conn, tenantId, adminId) = await TestDbFactory.CreateWithSeedSqliteAsync(UserRole.Admin);
        using var _conn = conn;

        // Add 3 more active employees (seeded user is Admin, not Employee, so 3 emp invitations)
        for (int i = 0; i < 3; i++)
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                FullName = $"Employee {i}", Email = $"emp{i}@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
                Role = UserRole.Employee, IsActive = true,
                CreatedBy = adminId, ModifiedBy = adminId,
            });
        }
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var emailSvc = new FakeEmailService();
        var sut = CreateService(db, currentUser, emailSvc);
        var survey = await SeedActiveSurveyAsync(db, tenantId, adminId);

        await sut.CreateInvitationsAndSendAsync(survey.Id, CancellationToken.None);

        // 3 active employees (Admin user has no Employee role bit)
        var count = db.SurveyInvitations.Count(i => i.SurveyId == survey.Id);
        Assert.Equal(3, count);
        Assert.Equal(3, emailSvc.SentEmails.Count);
    }

    // -- ResendToUserAsync -------------------------------------------------

    [Fact]
    public async Task ResendToUserAsync_AlreadySubmitted_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var adminId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = adminId, TenantId = tenantId, FullName = "Admin",
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ss1!"),
            Role = UserRole.Admin, IsActive = true,
            CreatedBy = userId, ModifiedBy = userId,
        });
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedActiveSurveyAsync(db, tenantId, adminId);
        await SeedInvitationAsync(db, tenantId, userId, survey.Id, SurveyInvitationStatus.Submitted);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.ResendToUserAsync(survey.Id, userId, CancellationToken.None));
    }

    [Fact]
    public async Task ResendToUserAsync_ClosedSurvey_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var adminId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = adminId, TenantId = tenantId, FullName = "Admin",
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ss1!"),
            Role = UserRole.Admin, IsActive = true,
            CreatedBy = userId, ModifiedBy = userId,
        });
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedActiveSurveyAsync(db, tenantId, adminId, SurveyStatus.Closed);
        await SeedInvitationAsync(db, tenantId, userId, survey.Id, SurveyInvitationStatus.Expired);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.ResendToUserAsync(survey.Id, userId, CancellationToken.None));
    }

    [Fact]
    public async Task ResendToUserAsync_GeneratesNewToken_OldStatusExpired()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var adminId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = adminId, TenantId = tenantId, FullName = "Admin",
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ss1!"),
            Role = UserRole.Admin, IsActive = true,
            CreatedBy = userId, ModifiedBy = userId,
        });
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var emailSvc = new FakeEmailService();
        var sut = CreateService(db, currentUser, emailSvc);
        var survey = await SeedActiveSurveyAsync(db, tenantId, adminId);
        var originalInv = await SeedInvitationAsync(db, tenantId, userId, survey.Id,
            SurveyInvitationStatus.Sent, DateTime.UtcNow.AddDays(-1)); // already expired

        var originalHash = originalInv.TokenHash;

        await sut.ResendToUserAsync(survey.Id, userId, CancellationToken.None);

        var updated = db.SurveyInvitations.First(i => i.Id == originalInv.Id);

        // New token hash should differ from old one
        Assert.NotEqual(originalHash, updated.TokenHash);
        // Status should be Sent (email succeeded since FakeEmailService doesn't throw)
        Assert.Equal(SurveyInvitationStatus.Sent, updated.Status);
        // ResendCount incremented
        Assert.Equal(1, updated.ResendCount);
        // Email was sent
        Assert.Single(emailSvc.SentEmails);
    }

    // -- ResendBulkAsync ---------------------------------------------------

    [Fact]
    public async Task ResendBulkAsync_SkipsSubmittedUsers()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);

        // Add 2 employees: one submitted, one expired
        var emp1Id = Guid.NewGuid();
        var emp2Id = Guid.NewGuid();
        foreach (var (id, name) in new[] { (emp1Id, "Alice"), (emp2Id, "Bob") })
        {
            db.Users.Add(new User
            {
                Id = id, TenantId = tenantId, FullName = name,
                Email = $"{name.ToLower()}@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ss1!"),
                Role = UserRole.Employee, IsActive = true,
                CreatedBy = adminId, ModifiedBy = adminId,
            });
        }
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var emailSvc = new FakeEmailService();
        var sut = CreateService(db, currentUser, emailSvc);
        var survey = await SeedActiveSurveyAsync(db, tenantId, adminId);

        await SeedInvitationAsync(db, tenantId, emp1Id, survey.Id, SurveyInvitationStatus.Submitted);
        await SeedInvitationAsync(db, tenantId, emp2Id, survey.Id, SurveyInvitationStatus.Sent);

        await sut.ResendBulkAsync(survey.Id,
            new ResendInvitationsRequest(new List<Guid> { emp1Id, emp2Id }),
            CancellationToken.None);

        // Only emp2 (non-submitted) should have received a new email
        Assert.Single(emailSvc.SentEmails);
    }

    // -- MarkExpiredAsync --------------------------------------------------

    [Fact]
    public async Task MarkExpiredAsync_OnlyExpiresSentBefore_UtcNow()
    {
        var (db, conn, tenantId, userId) = await TestDbFactory.CreateWithSeedSqliteAsync(UserRole.Employee);
        using var _conn = conn;
        var adminId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = adminId, TenantId = tenantId, FullName = "Admin",
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ss1!"),
            Role = UserRole.Admin, IsActive = true,
            CreatedBy = userId, ModifiedBy = userId,
        });
        await db.SaveChangesAsync();

        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedActiveSurveyAsync(db, tenantId, adminId);

        // Two invitations: one expired (ExpiresAt in past), one still valid
        var bytes1 = RandomNumberGenerator.GetBytes(32);
        var expiredInv = new SurveyInvitation
        {
            TenantId  = tenantId, SurveyId = survey.Id, UserId = userId,
            TokenHash = HashToken(Base64UrlTextEncoder.Encode(bytes1)),
            Status    = SurveyInvitationStatus.Sent,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // in the past
            CreatedBy = adminId, ModifiedBy = adminId,
        };
        db.SurveyInvitations.Add(expiredInv);

        var user2Id = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = user2Id, TenantId = tenantId, FullName = "User2",
            Email = "user2@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ss1!"),
            Role = UserRole.Employee, IsActive = true,
            CreatedBy = adminId, ModifiedBy = adminId,
        });

        var bytes2 = RandomNumberGenerator.GetBytes(32);
        var validInv = new SurveyInvitation
        {
            TenantId  = tenantId, SurveyId = survey.Id, UserId = user2Id,
            TokenHash = HashToken(Base64UrlTextEncoder.Encode(bytes2)),
            Status    = SurveyInvitationStatus.Sent,
            ExpiresAt = DateTime.UtcNow.AddDays(5), // still valid
            CreatedBy = adminId, ModifiedBy = adminId,
        };
        db.SurveyInvitations.Add(validInv);
        await db.SaveChangesAsync();

        await sut.MarkExpiredAsync(CancellationToken.None);

        // Must reload via AsNoTracking because ExecuteUpdateAsync bypasses change tracker
        Assert.Equal(SurveyInvitationStatus.Expired,
            db.SurveyInvitations.AsNoTracking().First(i => i.Id == expiredInv.Id).Status);
        Assert.Equal(SurveyInvitationStatus.Sent,
            db.SurveyInvitations.AsNoTracking().First(i => i.Id == validInv.Id).Status);
    }
}
