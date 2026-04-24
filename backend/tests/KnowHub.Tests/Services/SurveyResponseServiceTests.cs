using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KnowHub.Application.Models.Surveys;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services.Surveys;
using KnowHub.Tests.TestHelpers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KnowHub.Tests.Services;

public class SurveyResponseServiceTests
{
    // -- Helpers ------------------------------------------------------------

    private static SurveyResponseService CreateService(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        KnowHub.Application.Contracts.ICurrentUserAccessor? currentUser = null)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        currentUser ??= new FakeCurrentUserAccessor();
        return new SurveyResponseService(db, currentUser, cache);
    }

    private static string HashToken(string plainToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static (string plainToken, string tokenHash) GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var plain = Base64UrlTextEncoder.Encode(bytes);
        return (plain, HashToken(plain));
    }

    /// <summary>
    /// Seeds a fully valid scenario: Active survey, single question, valid Sent invitation.
    /// Returns the plain token to use in service calls.
    /// </summary>
    private static async Task<(Survey survey, SurveyQuestion question, SurveyInvitation invitation, string plainToken)>
        SeedValidScenarioAsync(
            KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
            Guid tenantId, Guid userId,
            SurveyStatus surveyStatus = SurveyStatus.Active,
            SurveyInvitationStatus invStatus = SurveyInvitationStatus.Sent,
            DateTime? expiresAt = null)
    {
        var survey = new Survey
        {
            TenantId        = tenantId,
            Title           = "Engagement Survey",
            WelcomeMessage  = "Welcome!",
            ThankYouMessage = "Thanks!",
            Status          = surveyStatus,
            TokenExpiryDays = 7,
            CreatedBy       = userId,
            ModifiedBy      = userId,
        };
        db.Surveys.Add(survey);

        var question = new SurveyQuestion
        {
            TenantId      = tenantId,
            SurveyId      = survey.Id,
            QuestionText  = "How do you rate your experience?",
            QuestionType  = SurveyQuestionType.Rating,
            MinRating     = 1,
            MaxRating     = 5,
            IsRequired    = true,
            OrderSequence = 0,
            CreatedBy     = userId,
            ModifiedBy    = userId,
        };
        db.SurveyQuestions.Add(question);
        await db.SaveChangesAsync();

        var (plainToken, tokenHash) = GenerateToken();
        var invitation = new SurveyInvitation
        {
            TenantId  = tenantId,
            SurveyId  = survey.Id,
            UserId    = userId,
            TokenHash = tokenHash,
            Status    = invStatus,
            SentAt    = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            CreatedBy = userId,
            ModifiedBy = userId,
        };
        db.SurveyInvitations.Add(invitation);
        await db.SaveChangesAsync();

        return (survey, question, invitation, plainToken);
    }

    // -- GetFormByTokenAsync ------------------------------------------------

    [Fact]
    public async Task GetFormByTokenAsync_ValidToken_Returns_SurveyForm()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var sut = CreateService(db);
        var (survey, q, _, plainToken) = await SeedValidScenarioAsync(db, tenantId, userId);

        var form = await sut.GetFormByTokenAsync(plainToken, CancellationToken.None);

        Assert.Equal(survey.Id, form.SurveyId);
        Assert.Equal("Engagement Survey", form.Title);
        Assert.Single(form.Questions);
        Assert.Equal(q.QuestionText, form.Questions[0].QuestionText);
    }

    [Fact]
    public async Task GetFormByTokenAsync_ExpiredToken_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var sut = CreateService(db);
        var (_, _, _, plainToken) = await SeedValidScenarioAsync(db, tenantId, userId,
            invStatus: SurveyInvitationStatus.Expired);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.GetFormByTokenAsync(plainToken, CancellationToken.None));
    }

    [Fact]
    public async Task GetFormByTokenAsync_PastExpiryDate_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var sut = CreateService(db);
        // Status is still Sent, but ExpiresAt is in the past
        var (_, _, _, plainToken) = await SeedValidScenarioAsync(db, tenantId, userId,
            invStatus: SurveyInvitationStatus.Sent,
            expiresAt: DateTime.UtcNow.AddDays(-1));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.GetFormByTokenAsync(plainToken, CancellationToken.None));
    }

    [Fact]
    public async Task GetFormByTokenAsync_AlreadySubmittedToken_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var sut = CreateService(db);
        var (_, _, _, plainToken) = await SeedValidScenarioAsync(db, tenantId, userId,
            invStatus: SurveyInvitationStatus.Submitted);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.GetFormByTokenAsync(plainToken, CancellationToken.None));
    }

    [Fact]
    public async Task GetFormByTokenAsync_UnknownToken_Throws_NotFoundException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var sut = CreateService(db);

        var (unknownToken, _) = GenerateToken();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.GetFormByTokenAsync(unknownToken, CancellationToken.None));
    }

    [Fact]
    public async Task GetFormByTokenAsync_ClosedSurvey_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var sut = CreateService(db);
        var (_, _, _, plainToken) = await SeedValidScenarioAsync(db, tenantId, userId,
            surveyStatus: SurveyStatus.Closed);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.GetFormByTokenAsync(plainToken, CancellationToken.None));
    }

    // -- SubmitAsync --------------------------------------------------------

    [Fact]
    public async Task SubmitAsync_ValidToken_AllRequired_Saves_Correctly()
    {
        var (db, conn, tenantId, userId) = await TestDbFactory.CreateWithSeedSqliteAsync(UserRole.Employee);
        using var sqliteConn = conn;
        var sut = CreateService(db);
        var (_, question, _, plainToken) = await SeedValidScenarioAsync(db, tenantId, userId);

        var request = new SubmitSurveyRequest(new List<SurveyAnswerRequest>
        {
            new(question.Id, null, null, 4)
        });

        var result = await sut.SubmitAsync(plainToken, request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(result.SubmittedAt > DateTime.UtcNow.AddMinutes(-1));

        // Invitation status should now be Submitted
        var inv = db.SurveyInvitations.First();
        Assert.Equal(SurveyInvitationStatus.Submitted, inv.Status);

        // Answer saved
        var answer = db.SurveyAnswers.First();
        Assert.Equal(4, answer.RatingValue);
    }

    [Fact]
    public async Task SubmitAsync_MissingRequiredAnswer_Throws_ValidationException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var sut = CreateService(db);
        var (_, _, _, plainToken) = await SeedValidScenarioAsync(db, tenantId, userId);

        // Submit with no answers
        var request = new SubmitSurveyRequest(new List<SurveyAnswerRequest>());

        await Assert.ThrowsAsync<KnowHub.Domain.Exceptions.ValidationException>(() =>
            sut.SubmitAsync(plainToken, request, CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAsync_InvalidRatingValue_Throws_ValidationException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var sut = CreateService(db);
        var (_, question, _, plainToken) = await SeedValidScenarioAsync(db, tenantId, userId);

        // Rating must be between 1 and 5; send 10 (out of range)
        var request = new SubmitSurveyRequest(new List<SurveyAnswerRequest>
        {
            new(question.Id, null, null, 10)
        });

        await Assert.ThrowsAsync<KnowHub.Domain.Exceptions.ValidationException>(() =>
            sut.SubmitAsync(plainToken, request, CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAsync_InvalidChoiceOption_Throws_ValidationException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var sut = CreateService(db);

        // Seed survey with SingleChoice question
        var survey = new Survey
        {
            TenantId = tenantId, Title = "Choice Survey", Status = SurveyStatus.Active,
            TokenExpiryDays = 7, CreatedBy = userId, ModifiedBy = userId,
        };
        db.Surveys.Add(survey);

        var options = new List<string> { "Option A", "Option B" };
        var choiceQ = new SurveyQuestion
        {
            TenantId = tenantId, SurveyId = survey.Id,
            QuestionText = "Pick one", QuestionType = SurveyQuestionType.SingleChoice,
            OptionsJson = JsonSerializer.Serialize(options),
            MinRating = 1, MaxRating = 5, IsRequired = true, OrderSequence = 0,
            CreatedBy = userId, ModifiedBy = userId,
        };
        db.SurveyQuestions.Add(choiceQ);
        await db.SaveChangesAsync();

        var (plainToken, tokenHash) = GenerateToken();
        db.SurveyInvitations.Add(new SurveyInvitation
        {
            TenantId = tenantId, SurveyId = survey.Id, UserId = userId,
            TokenHash = tokenHash, Status = SurveyInvitationStatus.Sent,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedBy = userId, ModifiedBy = userId,
        });
        await db.SaveChangesAsync();

        var request = new SubmitSurveyRequest(new List<SurveyAnswerRequest>
        {
            new(choiceQ.Id, "Invalid Option", null, null) // not in options list
        });

        await Assert.ThrowsAsync<KnowHub.Domain.Exceptions.ValidationException>(() =>
            sut.SubmitAsync(plainToken, request, CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAsync_DuplicateSubmission_Throws_ConflictException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var sut = CreateService(db);
        var (survey, question, invitation, plainToken) = await SeedValidScenarioAsync(db, tenantId, userId);

        // Insert an existing response to simulate duplicate
        db.SurveyResponses.Add(new SurveyResponse
        {
            TenantId     = tenantId, SurveyId = survey.Id, UserId = userId,
            InvitationId = invitation.Id, SubmittedAt = DateTime.UtcNow.AddMinutes(-5),
            CreatedBy = userId, ModifiedBy = userId,
        });
        await db.SaveChangesAsync();

        var request = new SubmitSurveyRequest(new List<SurveyAnswerRequest>
        {
            new(question.Id, null, null, 3)
        });

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.SubmitAsync(plainToken, request, CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAsync_ExpiredToken_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var sut = CreateService(db);
        var (_, question, _, plainToken) = await SeedValidScenarioAsync(db, tenantId, userId,
            expiresAt: DateTime.UtcNow.AddDays(-1));

        var request = new SubmitSurveyRequest(new List<SurveyAnswerRequest>
        {
            new(question.Id, null, null, 3)
        });

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.SubmitAsync(plainToken, request, CancellationToken.None));
    }
}
