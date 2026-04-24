using System.Text.Json;
using System.Threading.Channels;
using KnowHub.Application.Models.Surveys;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services.Surveys;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class SurveyServiceTests
{
    // -- Helpers ------------------------------------------------------------

    private static SurveyService CreateService(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        KnowHub.Application.Contracts.ICurrentUserAccessor currentUser,
        Channel<Guid>? channel = null)
    {
        channel ??= Channel.CreateUnbounded<Guid>();
        return new SurveyService(db, currentUser, channel);
    }

    private static CreateSurveyRequest ValidCreateRequest() =>
        new("Engagement Survey 2026", "Annual survey", null, null, null, false);

    private static async Task<Survey> SeedDraftSurveyAsync(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        Guid tenantId, Guid userId, string title = "Test Survey")
    {
        var survey = new Survey
        {
            TenantId        = tenantId,
            Title           = title,
            Status          = SurveyStatus.Draft,
            TokenExpiryDays = 7,
            CreatedBy       = userId,
            ModifiedBy      = userId,
        };
        db.Surveys.Add(survey);
        await db.SaveChangesAsync();
        return survey;
    }

    private static async Task<SurveyQuestion> SeedQuestionAsync(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        Guid tenantId, Guid userId, Guid surveyId,
        SurveyQuestionType type = SurveyQuestionType.Text,
        string? optionsJson = null)
    {
        var q = new SurveyQuestion
        {
            TenantId      = tenantId,
            SurveyId      = surveyId,
            QuestionText  = "How do you feel?",
            QuestionType  = type,
            OptionsJson   = optionsJson,
            MinRating     = 1,
            MaxRating     = 5,
            IsRequired    = true,
            OrderSequence = 0,
            CreatedBy     = userId,
            ModifiedBy    = userId,
        };
        db.SurveyQuestions.Add(q);
        await db.SaveChangesAsync();
        return q;
    }

    // -- CreateAsync --------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ValidRequest_Creates_DraftSurvey()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);

        var result = await sut.CreateAsync(ValidCreateRequest(), CancellationToken.None);

        Assert.Equal("Engagement Survey 2026", result.Title);
        Assert.Equal("Draft", result.Status);
        Assert.Equal(tenantId, result.TenantId);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_Throws_ValidationException()
    {
        // Validator-level check — the validator is invoked at the API level.
        // At service level, empty title is accepted (trimming), but we test the validator separately.
        // This test confirms the validator rejects it.
        var validator = new KnowHub.Application.Validators.Surveys.CreateSurveyRequestValidator();
        var result = await validator.ValidateAsync(new CreateSurveyRequest("", null, null, null, null, false));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task CreateAsync_AsEmployee_Throws_ForbiddenException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Employee);
        var currentUser = FakeCurrentUserAccessor.AsEmployee(userId, tenantId);
        var sut = CreateService(db, currentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.CreateAsync(ValidCreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_AsAdmin_Returns_Draft_Survey()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);

        var result = await sut.CreateAsync(ValidCreateRequest(), CancellationToken.None);

        Assert.Equal("Draft", result.Status);
        Assert.Equal(userId, result.CreatedBy);
    }

    // -- UpdateAsync --------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_DraftSurvey_Updates_Fields()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);

        var result = await sut.UpdateAsync(survey.Id,
            new UpdateSurveyRequest("Updated Title", "New Desc", null, null, null, 1),
            CancellationToken.None);

        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("New Desc", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_ActiveSurvey_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);
        survey.Status = SurveyStatus.Active;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.UpdateAsync(survey.Id,
                new UpdateSurveyRequest("X", null, null, null, null, 1),
                CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_StaleRecordVersion_Throws_ConflictException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);

        // RecordVersion=1 is correct; pass 99 to simulate stale
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.UpdateAsync(survey.Id,
                new UpdateSurveyRequest("X", null, null, null, null, 99),
                CancellationToken.None));
    }

    // -- DeleteAsync --------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_DraftSurvey_Succeeds()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);

        await sut.DeleteAsync(survey.Id, CancellationToken.None);

        Assert.False(db.Surveys.Any(s => s.Id == survey.Id));
    }

    [Fact]
    public async Task DeleteAsync_ActiveSurvey_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);
        survey.Status = SurveyStatus.Active;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.DeleteAsync(survey.Id, CancellationToken.None));
    }

    // -- LaunchAsync --------------------------------------------------------

    [Fact]
    public async Task LaunchAsync_DraftSurvey_Returns_ActiveStatus()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var channel = Channel.CreateUnbounded<Guid>();
        var sut = CreateService(db, currentUser, channel);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);
        await SeedQuestionAsync(db, tenantId, userId, survey.Id);

        var result = await sut.LaunchAsync(survey.Id, CancellationToken.None);

        Assert.Equal("Active", result.Status);
        Assert.NotNull(result.LaunchedAt);
    }

    [Fact]
    public async Task LaunchAsync_NoQuestions_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.LaunchAsync(survey.Id, CancellationToken.None));
    }

    [Fact]
    public async Task LaunchAsync_ActiveSurvey_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);
        await SeedQuestionAsync(db, tenantId, userId, survey.Id);
        survey.Status = SurveyStatus.Active;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.LaunchAsync(survey.Id, CancellationToken.None));
    }

    // -- CloseAsync --------------------------------------------------------

    [Fact]
    public async Task CloseAsync_ActiveSurvey_Returns_Closed_Status()
    {
        var (db, conn, tenantId, userId) = await TestDbFactory.CreateWithSeedSqliteAsync(UserRole.Admin);
        using var _ = conn;
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);
        survey.Status = SurveyStatus.Active;
        await db.SaveChangesAsync();

        var result = await sut.CloseAsync(survey.Id, CancellationToken.None);

        Assert.Equal("Closed", result.Status);
        Assert.NotNull(result.ClosedAt);
    }

    [Fact]
    public async Task CloseAsync_DraftSurvey_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.CloseAsync(survey.Id, CancellationToken.None));
    }

    // -- AddQuestionAsync --------------------------------------------------

    [Fact]
    public async Task AddQuestionAsync_DraftSurvey_Succeeds()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);

        var result = await sut.AddQuestionAsync(survey.Id,
            new AddSurveyQuestionRequest("Rate your satisfaction", SurveyQuestionType.Rating,
                null, 1, 5, true, 0),
            CancellationToken.None);

        Assert.Equal("Rate your satisfaction", result.QuestionText);
        Assert.Equal("Rating", result.QuestionType);
    }

    [Fact]
    public async Task AddQuestionAsync_ChoiceType_Without_Options_Throws_ValidationException()
    {
        // Validator-level test
        var validator = new KnowHub.Application.Validators.Surveys.AddSurveyQuestionRequestValidator();
        var result = await validator.ValidateAsync(new AddSurveyQuestionRequest(
            "Pick one", SurveyQuestionType.SingleChoice, null, 1, 5, true, 0));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AddQuestionAsync_ActiveSurvey_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);
        survey.Status = SurveyStatus.Active;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.AddQuestionAsync(survey.Id,
                new AddSurveyQuestionRequest("Q?", SurveyQuestionType.Text, null, 1, 5, true, 0),
                CancellationToken.None));
    }

    // -- DeleteQuestionAsync -----------------------------------------------

    [Fact]
    public async Task DeleteQuestionAsync_DraftSurvey_NoAnswers_Succeeds()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);
        var q = await SeedQuestionAsync(db, tenantId, userId, survey.Id);

        await sut.DeleteQuestionAsync(survey.Id, q.Id, CancellationToken.None);

        Assert.False(db.SurveyQuestions.Any(x => x.Id == q.Id));
    }

    [Fact]
    public async Task DeleteQuestionAsync_DraftSurvey_HasAnswers_Throws_ConflictException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);
        var q = await SeedQuestionAsync(db, tenantId, userId, survey.Id);

        // Seed a SurveyResponse + SurveyAnswer referencing this question (simulate answered)
        var response = new SurveyResponse
        {
            TenantId     = tenantId,
            SurveyId     = survey.Id,
            UserId       = userId,
            InvitationId = Guid.NewGuid(), // FK not enforced in InMemory
            SubmittedAt  = DateTime.UtcNow,
            CreatedBy    = userId,
            ModifiedBy   = userId,
        };
        db.SurveyResponses.Add(response);
        await db.SaveChangesAsync();

        var answer = new SurveyAnswer
        {
            TenantId    = tenantId,
            ResponseId  = response.Id,
            QuestionId  = q.Id,
            AnswerText  = "Good",
            CreatedBy   = userId,
            ModifiedBy  = userId,
        };
        db.SurveyAnswers.Add(answer);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.DeleteQuestionAsync(survey.Id, q.Id, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteQuestionAsync_ActiveSurvey_Throws_BusinessRuleException()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);
        var q = await SeedQuestionAsync(db, tenantId, userId, survey.Id);
        survey.Status = SurveyStatus.Active;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.DeleteQuestionAsync(survey.Id, q.Id, CancellationToken.None));
    }

    // -- ReorderQuestionsAsync ---------------------------------------------

    [Fact]
    public async Task ReorderQuestionsAsync_UpdatesSequence()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);

        var q1 = await SeedQuestionAsync(db, tenantId, userId, survey.Id);
        var q2 = new SurveyQuestion
        {
            TenantId = tenantId, SurveyId = survey.Id, QuestionText = "Q2",
            QuestionType = SurveyQuestionType.Text, MinRating = 1, MaxRating = 5,
            OrderSequence = 1, CreatedBy = userId, ModifiedBy = userId,
        };
        db.SurveyQuestions.Add(q2);
        await db.SaveChangesAsync();

        // Reverse order: q2 first, then q1
        await sut.ReorderQuestionsAsync(survey.Id,
            new ReorderQuestionsRequest(new List<Guid> { q2.Id, q1.Id }),
            CancellationToken.None);

        var updated = db.SurveyQuestions.ToList();
        Assert.Equal(0, updated.First(q => q.Id == q2.Id).OrderSequence);
        Assert.Equal(1, updated.First(q => q.Id == q1.Id).OrderSequence);
    }

    // -- GetResultsAsync ---------------------------------------------------

    [Fact]
    public async Task GetResultsAsync_AggregatesCorrectly()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);

        var survey = new Survey
        {
            TenantId = tenantId, Title = "Survey", Status = SurveyStatus.Active,
            TokenExpiryDays = 7, TotalInvited = 10, TotalResponded = 4,
            CreatedBy = userId, ModifiedBy = userId,
        };
        db.Surveys.Add(survey);

        var ratingQ = new SurveyQuestion
        {
            TenantId = tenantId, SurveyId = survey.Id, QuestionText = "Rate us",
            QuestionType = SurveyQuestionType.Rating, MinRating = 1, MaxRating = 5,
            OrderSequence = 0, CreatedBy = userId, ModifiedBy = userId,
        };
        db.SurveyQuestions.Add(ratingQ);
        await db.SaveChangesAsync();

        // Seed 4 responses with ratings: 3,4,5,4
        var ratings = new[] { 3, 4, 5, 4 };
        foreach (var rating in ratings)
        {
            var resp = new SurveyResponse
            {
                TenantId = tenantId, SurveyId = survey.Id, UserId = Guid.NewGuid(),
                InvitationId = Guid.NewGuid(), SubmittedAt = DateTime.UtcNow,
                CreatedBy = userId, ModifiedBy = userId,
            };
            db.SurveyResponses.Add(resp);
            await db.SaveChangesAsync();

            db.SurveyAnswers.Add(new SurveyAnswer
            {
                TenantId = tenantId, ResponseId = resp.Id, QuestionId = ratingQ.Id,
                RatingValue = rating, CreatedBy = userId, ModifiedBy = userId,
            });
        }
        await db.SaveChangesAsync();

        var results = await sut.GetResultsAsync(survey.Id, CancellationToken.None);

        Assert.Equal(40, results.ResponseRatePercent); // 4/10
        Assert.Single(results.QuestionResults);
        var qResult = results.QuestionResults[0];
        Assert.Equal(4, qResult.TotalAnswers);
        Assert.NotNull(qResult.AverageRating);
        Assert.Equal(4.0, qResult.AverageRating!.Value, precision: 1); // (3+4+5+4)/4
    }

    // -- CopyAsync ---------------------------------------------------------

    [Fact]
    public async Task CopyAsync_AllQuestions_Creates_NewDraftWithSameQuestions()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId, "Original");
        await SeedQuestionAsync(db, tenantId, userId, survey.Id);
        await SeedQuestionAsync(db, tenantId, userId, survey.Id);

        var copy = await sut.CopyAsync(survey.Id,
            new CopySurveyRequest(null, new List<Guid>()),
            CancellationToken.None);

        Assert.Equal("Draft", copy.Status);
        Assert.Equal("Copy of Original", copy.Title);
        Assert.Equal(2, copy.Questions.Count);
    }

    [Fact]
    public async Task CopyAsync_WithExcludeQuestionIds_OmitsThoseQuestions()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId);
        var q1 = await SeedQuestionAsync(db, tenantId, userId, survey.Id);
        var q2 = new SurveyQuestion
        {
            TenantId = tenantId, SurveyId = survey.Id, QuestionText = "Q2",
            QuestionType = SurveyQuestionType.Text, MinRating = 1, MaxRating = 5,
            OrderSequence = 1, CreatedBy = userId, ModifiedBy = userId,
        };
        db.SurveyQuestions.Add(q2);
        await db.SaveChangesAsync();

        var copy = await sut.CopyAsync(survey.Id,
            new CopySurveyRequest(null, new List<Guid> { q1.Id }),
            CancellationToken.None);

        Assert.Single(copy.Questions);
        Assert.DoesNotContain(copy.Questions, q => q.QuestionText == q1.QuestionText);
    }

    [Fact]
    public async Task CopyAsync_DefaultTitle_Prefixed_CopyOf()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId, "My Survey");

        var copy = await sut.CopyAsync(survey.Id,
            new CopySurveyRequest(null, new List<Guid>()),
            CancellationToken.None);

        Assert.Equal("Copy of My Survey", copy.Title);
    }

    [Fact]
    public async Task CopyAsync_CustomNewTitle_UsesProvidedTitle()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId, "Original");

        var copy = await sut.CopyAsync(survey.Id,
            new CopySurveyRequest("Brand New Title", new List<Guid>()),
            CancellationToken.None);

        Assert.Equal("Brand New Title", copy.Title);
    }

    [Fact]
    public async Task CopyAsync_ExcludeQuestionIds_NotInSource_AreIgnored()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedDraftSurveyAsync(db, tenantId, userId, "Survey");
        await SeedQuestionAsync(db, tenantId, userId, survey.Id);

        // Pass a random Guid that doesn't belong to this survey
        var randomId = Guid.NewGuid();
        var copy = await sut.CopyAsync(survey.Id,
            new CopySurveyRequest(null, new List<Guid> { randomId }),
            CancellationToken.None);

        // All 1 questions should still be copied since the exclude ID is irrelevant
        Assert.Single(copy.Questions);
    }
}
