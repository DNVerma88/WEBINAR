using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KnowHub.Application.Models.Surveys.Analytics;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services.Surveys;
using KnowHub.Tests.TestHelpers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowHub.Tests.Services;

public class SurveyAnalyticsServiceTests
{
    // -- Helpers ------------------------------------------------------------

    private static SurveyAnalyticsService CreateService(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        KnowHub.Application.Contracts.ICurrentUserAccessor currentUser)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new SurveyAnalyticsService(
            db, currentUser, cache,
            NullLogger<SurveyAnalyticsService>.Instance);
    }

    private static string GenerateTokenHash()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var plain = Base64UrlTextEncoder.Encode(bytes);
        var hash  = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Creates a survey + optional questions, returning the survey entity.</summary>
    private static async Task<Survey> SeedSurveyAsync(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        Guid tenantId, Guid userId,
        string title = "Survey",
        SurveyStatus status = SurveyStatus.Active,
        int totalInvited = 0, int totalResponded = 0,
        DateTime? launchedAt = null)
    {
        var survey = new Survey
        {
            TenantId        = tenantId,
            Title           = title,
            Status          = status,
            TokenExpiryDays = 7,
            TotalInvited    = totalInvited,
            TotalResponded  = totalResponded,
            LaunchedAt      = launchedAt ?? DateTime.UtcNow.AddDays(-7),
            CreatedBy       = userId,
            ModifiedBy      = userId,
        };
        db.Surveys.Add(survey);
        await db.SaveChangesAsync();
        return survey;
    }

    private static async Task<SurveyQuestion> SeedRatingQuestionAsync(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        Guid tenantId, Guid userId, Guid surveyId,
        string text = "Rate us", int min = 1, int max = 5, int order = 0)
    {
        var q = new SurveyQuestion
        {
            TenantId      = tenantId,
            SurveyId      = surveyId,
            QuestionText  = text,
            QuestionType  = SurveyQuestionType.Rating,
            MinRating     = min,
            MaxRating     = max,
            IsRequired    = true,
            OrderSequence = order,
            CreatedBy     = userId,
            ModifiedBy    = userId,
        };
        db.SurveyQuestions.Add(q);
        await db.SaveChangesAsync();
        return q;
    }

    private static async Task<(SurveyResponse response, SurveyInvitation invitation)> SeedResponseAsync(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        Guid tenantId, Guid userId, Guid surveyId,
        DateTime? submittedAt = null,
        DateTime? tokenAccessedAt = null)
    {
        var invitation = new SurveyInvitation
        {
            TenantId         = tenantId,
            SurveyId         = surveyId,
            UserId           = userId,
            TokenHash        = GenerateTokenHash(),
            Status           = SurveyInvitationStatus.Submitted,
            SentAt           = DateTime.UtcNow.AddDays(-2),
            ExpiresAt        = DateTime.UtcNow.AddDays(5),
            SubmittedAt      = submittedAt ?? DateTime.UtcNow,
            TokenAccessedAt  = tokenAccessedAt,
            CreatedBy        = userId,
            ModifiedBy       = userId,
        };
        db.SurveyInvitations.Add(invitation);

        var response = new SurveyResponse
        {
            TenantId     = tenantId,
            SurveyId     = surveyId,
            UserId       = userId,
            InvitationId = invitation.Id,
            SubmittedAt  = submittedAt ?? DateTime.UtcNow,
            CreatedBy    = userId,
            ModifiedBy   = userId,
        };
        db.SurveyResponses.Add(response);
        await db.SaveChangesAsync();
        return (response, invitation);
    }

    private static async Task SeedRatingAnswerAsync(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        Guid tenantId, Guid responseId, Guid questionId, int rating, Guid userId)
    {
        db.SurveyAnswers.Add(new SurveyAnswer
        {
            TenantId    = tenantId,
            ResponseId  = responseId,
            QuestionId  = questionId,
            RatingValue = rating,
            CreatedBy   = userId,
            ModifiedBy  = userId,
        });
        await db.SaveChangesAsync();
    }

    private static async Task<User> SeedUserAsync(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        Guid tenantId, Guid adminId,
        string dept = "Engineering", string name = "emptemp")
    {
        var user = new User
        {
            Id           = Guid.NewGuid(),
            TenantId     = tenantId,
            FullName     = name,
            Email        = $"{Guid.NewGuid():N}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
            Role         = UserRole.Employee,
            IsActive     = true,
            Department   = dept,
            CreatedBy    = adminId,
            ModifiedBy   = adminId,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // -- GetDashboardAsync --------------------------------------------------

    [Fact]
    public async Task GetDashboardAsync_50PercentResponseRate_Returns_AtRiskHealth()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedSurveyAsync(db, tenantId, userId, totalInvited: 10, totalResponded: 5);

        var result = await sut.GetDashboardAsync(survey.Id);

        Assert.Equal(50.0, result.ResponseRatePct);
        Assert.Equal("AtRisk", result.HealthStatus);
    }

    [Fact]
    public async Task GetDashboardAsync_70PercentResponseRate_Returns_HealthyStatus()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedSurveyAsync(db, tenantId, userId, totalInvited: 10, totalResponded: 7);

        var result = await sut.GetDashboardAsync(survey.Id);

        Assert.Equal(70.0, result.ResponseRatePct);
        Assert.Equal("Healthy", result.HealthStatus);
    }

    // -- GetQuestionStatsAsync ----------------------------------------------

    [Fact]
    public async Task GetQuestionStatsAsync_RatingQuestion_Returns_AvgAndDistribution()
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(userId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedSurveyAsync(db, tenantId, userId, totalInvited: 5, totalResponded: 5);
        var q = await SeedRatingQuestionAsync(db, tenantId, userId, survey.Id, min: 1, max: 10);

        var ratings = new[] { 8, 9, 9, 10, 7 };
        foreach (var rating in ratings)
        {
            var respUser = await SeedUserAsync(db, tenantId, userId);
            var (resp, _) = await SeedResponseAsync(db, tenantId, respUser.Id, survey.Id);
            await SeedRatingAnswerAsync(db, tenantId, resp.Id, q.Id, rating, userId);
        }

        var stats = await sut.GetQuestionStatsAsync(survey.Id, null, null, null, null);

        Assert.Single(stats);
        var qs = stats[0];
        Assert.Equal(5, qs.TotalAnswers);
        Assert.NotNull(qs.AverageRating);
        Assert.Equal(8.6, qs.AverageRating!.Value, precision: 1);
        // Distribution should have entries for 7,8,9,10
        Assert.Contains(qs.OptionStats, o => o.OptionValue == "9" && o.Count == 2);
        Assert.Contains(qs.OptionStats, o => o.OptionValue == "8" && o.Count == 1);
    }

    [Fact]
    public async Task GetQuestionStatsAsync_DepartmentFilter_Returns_OnlyFilteredDeptAnswers()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedSurveyAsync(db, tenantId, adminId, totalInvited: 3, totalResponded: 3);
        var q = await SeedRatingQuestionAsync(db, tenantId, adminId, survey.Id, min: 1, max: 5);

        var engUser  = await SeedUserAsync(db, tenantId, adminId, dept: "Engineering", name: "Eng");
        var hrUser1  = await SeedUserAsync(db, tenantId, adminId, dept: "HR", name: "HR1");
        var hrUser2  = await SeedUserAsync(db, tenantId, adminId, dept: "HR", name: "HR2");

        var (respEng, _) = await SeedResponseAsync(db, tenantId, engUser.Id, survey.Id);
        await SeedRatingAnswerAsync(db, tenantId, respEng.Id, q.Id, 5, adminId);

        var (respHr1, _) = await SeedResponseAsync(db, tenantId, hrUser1.Id, survey.Id);
        await SeedRatingAnswerAsync(db, tenantId, respHr1.Id, q.Id, 3, adminId);

        var (respHr2, _) = await SeedResponseAsync(db, tenantId, hrUser2.Id, survey.Id);
        await SeedRatingAnswerAsync(db, tenantId, respHr2.Id, q.Id, 4, adminId);

        var stats = await sut.GetQuestionStatsAsync(survey.Id, "Engineering", null, null, null);

        Assert.Single(stats);
        // Only Engineering user's answer (5) should be counted
        Assert.Equal(1, stats[0].TotalAnswers);
        Assert.Equal(5.0, stats[0].AverageRating!.Value, precision: 1);
    }

    // -- GetNpsReportAsync --------------------------------------------------

    [Fact]
    public async Task GetNpsReportAsync_MixedRatings_Returns_CorrectNpsScore()
    {
        // 4 promoters (9,10), 2 passives (7,8), 4 detractors (0-6)
        // NPS = (4-4)/10 * 100 = 0
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedSurveyAsync(db, tenantId, adminId, totalInvited: 10, totalResponded: 10);
        var npsQ = await SeedRatingQuestionAsync(db, tenantId, adminId, survey.Id, min: 0, max: 10);

        // Promoters: 9,9,10,10
        // Passives: 7,8
        // Detractors: 1,2,3,4
        var ratingsList = new[] { 9, 9, 10, 10, 7, 8, 1, 2, 3, 4 };
        foreach (var r in ratingsList)
        {
            var u = await SeedUserAsync(db, tenantId, adminId);
            var (resp, _) = await SeedResponseAsync(db, tenantId, u.Id, survey.Id);
            await SeedRatingAnswerAsync(db, tenantId, resp.Id, npsQ.Id, r, adminId);
        }

        var nps = await sut.GetNpsReportAsync(survey.Id);

        Assert.Equal(4, nps.Promoters);
        Assert.Equal(2, nps.Passives);
        Assert.Equal(4, nps.Detractors);
        Assert.Equal(0, nps.NpsScore);  // (4-4)/10*100 = 0
    }

    [Fact]
    public async Task GetNpsReportAsync_AllPromoters_Returns_NpsScore100()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedSurveyAsync(db, tenantId, adminId, totalInvited: 5, totalResponded: 5);
        var npsQ = await SeedRatingQuestionAsync(db, tenantId, adminId, survey.Id, min: 0, max: 10);

        foreach (var r in new[] { 9, 10, 9, 10, 10 })
        {
            var u = await SeedUserAsync(db, tenantId, adminId);
            var (resp, _) = await SeedResponseAsync(db, tenantId, u.Id, survey.Id);
            await SeedRatingAnswerAsync(db, tenantId, resp.Id, npsQ.Id, r, adminId);
        }

        var nps = await sut.GetNpsReportAsync(survey.Id);

        Assert.Equal(5, nps.Promoters);
        Assert.Equal(0, nps.Detractors);
        Assert.Equal(100, nps.NpsScore);
    }

    [Fact]
    public async Task GetNpsReportAsync_NoNpsQuestion_Throws_BusinessRuleException()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedSurveyAsync(db, tenantId, adminId);
        // Add a 1-5 rating question (not NPS)
        await SeedRatingQuestionAsync(db, tenantId, adminId, survey.Id, min: 1, max: 5);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.GetNpsReportAsync(survey.Id));
    }

    // -- GetParticipationFunnelAsync ----------------------------------------

    [Fact]
    public async Task GetParticipationFunnelAsync_Returns_CorrectStageCounts()
    {
        // 10 invited, 9 sent, 7 accessed, 5 submitted
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedSurveyAsync(db, tenantId, adminId,
            totalInvited: 10, totalResponded: 5);

        // Seed 9 invitations with various statuses
        for (int i = 0; i < 9; i++)
        {
            var u = await SeedUserAsync(db, tenantId, adminId, name: $"user{i}");
            var accessed  = i < 7 ? DateTime.UtcNow.AddHours(-1) : (DateTime?)null;
            var status    = i < 5 ? SurveyInvitationStatus.Submitted : SurveyInvitationStatus.Sent;
            db.SurveyInvitations.Add(new SurveyInvitation
            {
                TenantId        = tenantId,
                SurveyId        = survey.Id,
                UserId          = u.Id,
                TokenHash       = GenerateTokenHash(),
                Status          = status,
                SentAt          = DateTime.UtcNow.AddDays(-2),
                ExpiresAt       = DateTime.UtcNow.AddDays(5),
                TokenAccessedAt = accessed,
                CreatedBy       = adminId,
                ModifiedBy      = adminId,
            });
        }
        // 10th invitation in Pending state (not sent yet)
        var uPending = await SeedUserAsync(db, tenantId, adminId, name: "pending");
        db.SurveyInvitations.Add(new SurveyInvitation
        {
            TenantId  = tenantId, SurveyId = survey.Id, UserId = uPending.Id,
            TokenHash = GenerateTokenHash(), Status = SurveyInvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            CreatedBy = adminId, ModifiedBy = adminId,
        });
        await db.SaveChangesAsync();

        var funnel = await sut.GetParticipationFunnelAsync(survey.Id);

        Assert.Equal(10, funnel.TotalInvited);
        Assert.Equal(9, funnel.TotalEmailsSent);      // 9 non-Pending
        Assert.Equal(7, funnel.TotalTokensAccessed);  // 7 with TokenAccessedAt set
        Assert.Equal(5, funnel.TotalSubmitted);
    }

    // -- GetHeatmapAsync ----------------------------------------------------

    [Fact]
    public async Task GetHeatmapAsync_TwoDepts_TwoRatingQuestions_Returns_CorrectMatrix()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedSurveyAsync(db, tenantId, adminId, totalInvited: 4, totalResponded: 4);
        var q1 = await SeedRatingQuestionAsync(db, tenantId, adminId, survey.Id, text: "Q1 satisfaction", order: 0);
        var q2 = await SeedRatingQuestionAsync(db, tenantId, adminId, survey.Id, text: "Q2 clarity", order: 1);

        // Engineering: Q1=9, Q1=8 → avg 8.5; Q2=7, Q2=6 → avg 6.5
        var engUser1 = await SeedUserAsync(db, tenantId, adminId, dept: "Engineering", name: "Eng1");
        var engUser2 = await SeedUserAsync(db, tenantId, adminId, dept: "Engineering", name: "Eng2");
        // HR: Q1=7 → avg 7.0; Q2=8 → avg 8.0
        var hrUser = await SeedUserAsync(db, tenantId, adminId, dept: "HR", name: "HR1");

        var (respEng1, _) = await SeedResponseAsync(db, tenantId, engUser1.Id, survey.Id);
        await SeedRatingAnswerAsync(db, tenantId, respEng1.Id, q1.Id, 9, adminId);
        await SeedRatingAnswerAsync(db, tenantId, respEng1.Id, q2.Id, 7, adminId);

        var (respEng2, _) = await SeedResponseAsync(db, tenantId, engUser2.Id, survey.Id);
        await SeedRatingAnswerAsync(db, tenantId, respEng2.Id, q1.Id, 8, adminId);
        await SeedRatingAnswerAsync(db, tenantId, respEng2.Id, q2.Id, 6, adminId);

        var (respHr, _) = await SeedResponseAsync(db, tenantId, hrUser.Id, survey.Id);
        await SeedRatingAnswerAsync(db, tenantId, respHr.Id, q1.Id, 7, adminId);
        await SeedRatingAnswerAsync(db, tenantId, respHr.Id, q2.Id, 8, adminId);

        var heatmap = await sut.GetHeatmapAsync(survey.Id);

        Assert.Contains("Engineering", heatmap.Departments);
        Assert.Contains("HR", heatmap.Departments);
        Assert.Equal(2, heatmap.QuestionTexts.Count);

        var engIdx = ((IReadOnlyList<string>)heatmap.Departments).ToList().IndexOf("Engineering");
        var hrIdx  = ((IReadOnlyList<string>)heatmap.Departments).ToList().IndexOf("HR");
        var q1Idx  = ((IReadOnlyList<string>)heatmap.QuestionTexts).ToList().IndexOf("Q1 satisfaction");
        var q2Idx  = ((IReadOnlyList<string>)heatmap.QuestionTexts).ToList().IndexOf("Q2 clarity");

        Assert.Equal(8.5, heatmap.Matrix[engIdx][q1Idx], precision: 1);
        Assert.Equal(6.5, heatmap.Matrix[engIdx][q2Idx], precision: 1);
        Assert.Equal(7.0, heatmap.Matrix[hrIdx][q1Idx],  precision: 1);
        Assert.Equal(8.0, heatmap.Matrix[hrIdx][q2Idx],  precision: 1);
    }

    [Fact]
    public async Task GetHeatmapAsync_NonRatingQuestions_Excluded()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedSurveyAsync(db, tenantId, adminId);

        // Add only a Text question (no Rating questions)
        db.SurveyQuestions.Add(new SurveyQuestion
        {
            TenantId = tenantId, SurveyId = survey.Id,
            QuestionText = "Comments", QuestionType = SurveyQuestionType.Text,
            MinRating = 1, MaxRating = 5, OrderSequence = 0,
            CreatedBy = adminId, ModifiedBy = adminId,
        });
        await db.SaveChangesAsync();

        var heatmap = await sut.GetHeatmapAsync(survey.Id);

        // No rating questions → empty heatmap
        Assert.Empty(heatmap.QuestionTexts);
        Assert.Empty(heatmap.Departments);
    }

    // -- CompareSurveysAsync ------------------------------------------------

    [Fact]
    public async Task CompareSurveysAsync_SharedQuestionByText_Returns_MatchedQuestion()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);

        var surveyA = await SeedSurveyAsync(db, tenantId, adminId, title: "Survey A");
        var surveyB = await SeedSurveyAsync(db, tenantId, adminId, title: "Survey B");

        // Shared question text (case-insensitive lowercase match)
        await SeedRatingQuestionAsync(db, tenantId, adminId, surveyA.Id, text: "How satisfied are you");
        await SeedRatingQuestionAsync(db, tenantId, adminId, surveyB.Id, text: "How satisfied are you");

        var comparison = await sut.CompareSurveysAsync(surveyA.Id, surveyB.Id);

        Assert.Single(comparison.SharedQuestions);
        Assert.Equal("how satisfied are you", comparison.SharedQuestions[0].QuestionText);
    }

    [Fact]
    public async Task CompareSurveysAsync_NoSharedQuestions_Returns_EmptySharedList()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);

        var surveyA = await SeedSurveyAsync(db, tenantId, adminId, title: "Survey A");
        var surveyB = await SeedSurveyAsync(db, tenantId, adminId, title: "Survey B");

        await SeedRatingQuestionAsync(db, tenantId, adminId, surveyA.Id, text: "Question unique to A");
        await SeedRatingQuestionAsync(db, tenantId, adminId, surveyB.Id, text: "Question unique to B");

        var comparison = await sut.CompareSurveysAsync(surveyA.Id, surveyB.Id);

        Assert.Empty(comparison.SharedQuestions);
    }

    // -- ExportToCsvAsync ---------------------------------------------------

    [Fact]
    public async Task ExportToCsvAsync_AnonymousSurvey_DoesNotIncludeRespondentColumns()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);

        // Anonymous survey
        var survey = new Survey
        {
            TenantId = tenantId, Title = "Anon Survey", Status = SurveyStatus.Closed,
            TokenExpiryDays = 7, IsAnonymous = true,
            CreatedBy = adminId, ModifiedBy = adminId,
        };
        db.Surveys.Add(survey);
        await db.SaveChangesAsync();

        var (data, _) = await sut.ExportToCsvAsync(
            new SurveyExportRequest(survey.Id, ExportFormat.Csv, true, null, null));

        var csv = System.Text.Encoding.UTF8.GetString(data);
        // PII columns must NOT appear even when IncludeRespondentInfo = true
        Assert.DoesNotContain("RespondentName", csv);
        Assert.DoesNotContain("RespondentEmail", csv);
        // Basic column should still appear
        Assert.Contains("ResponseId", csv);
    }

    [Fact]
    public async Task ExportToCsvAsync_NonAnonymous_IncludeRespondents_ContainsPiiColumns()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);

        var survey = new Survey
        {
            TenantId = tenantId, Title = "Non-Anon Survey", Status = SurveyStatus.Closed,
            TokenExpiryDays = 7, IsAnonymous = false,
            CreatedBy = adminId, ModifiedBy = adminId,
        };
        db.Surveys.Add(survey);

        var q = new SurveyQuestion
        {
            TenantId = tenantId, SurveyId = survey.Id, QuestionText = "Rate",
            QuestionType = SurveyQuestionType.Rating, MinRating = 1, MaxRating = 5,
            OrderSequence = 0, CreatedBy = adminId, ModifiedBy = adminId,
        };
        db.SurveyQuestions.Add(q);
        await db.SaveChangesAsync();

        // Add a response with a real user
        var respUser = await SeedUserAsync(db, tenantId, adminId, name: "John Doe");
        db.SurveyResponses.Add(new SurveyResponse
        {
            TenantId = tenantId, SurveyId = survey.Id, UserId = respUser.Id,
            InvitationId = Guid.NewGuid(), SubmittedAt = DateTime.UtcNow,
            CreatedBy = adminId, ModifiedBy = adminId,
        });
        await db.SaveChangesAsync();

        var (data, _) = await sut.ExportToCsvAsync(
            new SurveyExportRequest(survey.Id, ExportFormat.Csv, true, null, null));

        var csv = System.Text.Encoding.UTF8.GetString(data);
        Assert.Contains("RespondentName", csv);
        Assert.Contains("RespondentEmail", csv);
    }

    [Fact]
    public async Task ExportToCsvAsync_CsvInjection_ValuesAreSanitised()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);

        var survey = new Survey
        {
            TenantId = tenantId, Title = "=Injection Test", Status = SurveyStatus.Closed,
            TokenExpiryDays = 7, IsAnonymous = false,
            CreatedBy = adminId, ModifiedBy = adminId,
        };
        db.Surveys.Add(survey);

        var q = new SurveyQuestion
        {
            TenantId = tenantId, SurveyId = survey.Id,
            QuestionText = "=CMD|' /C calc'!A0'",  // CSV injection attempt in question
            QuestionType = SurveyQuestionType.Text,
            MinRating = 1, MaxRating = 5, OrderSequence = 0,
            CreatedBy = adminId, ModifiedBy = adminId,
        };
        db.SurveyQuestions.Add(q);
        await db.SaveChangesAsync();

        var respUser = await SeedUserAsync(db, tenantId, adminId);
        var response = new SurveyResponse
        {
            TenantId = tenantId, SurveyId = survey.Id, UserId = respUser.Id,
            InvitationId = Guid.NewGuid(), SubmittedAt = DateTime.UtcNow,
            CreatedBy = adminId, ModifiedBy = adminId,
        };
        db.SurveyResponses.Add(response);
        await db.SaveChangesAsync();

        // Answer with injection payload
        db.SurveyAnswers.Add(new SurveyAnswer
        {
            TenantId = tenantId, ResponseId = response.Id, QuestionId = q.Id,
            AnswerText = "=SUM(1+1)*cmd|' /C calc'!A0",
            CreatedBy = adminId, ModifiedBy = adminId,
        });
        await db.SaveChangesAsync();

        var (data, _) = await sut.ExportToCsvAsync(
            new SurveyExportRequest(survey.Id, ExportFormat.Csv, false, null, null));

        var csv = System.Text.Encoding.UTF8.GetString(data);

        // Sanitised values must NOT start with = after the opening quote
        // The SanitiseCsvValue method should prefix with \t (tab) or ' character
        Assert.DoesNotContain(",=SUM", csv);
        Assert.DoesNotContain(",=CMD", csv);
    }

    // -- GetNpsTrendAsync ---------------------------------------------------

    [Fact]
    public async Task GetNpsTrendAsync_MultipleSurveys_Returns_ChronologicalOrder()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);

        // Create 3 surveys with different LaunchedAt dates
        var surveyOld    = await SeedSurveyAsync(db, tenantId, adminId, title: "Old",
            launchedAt: DateTime.UtcNow.AddDays(-30));
        var surveyMiddle = await SeedSurveyAsync(db, tenantId, adminId, title: "Middle",
            launchedAt: DateTime.UtcNow.AddDays(-15));
        var surveyNew    = await SeedSurveyAsync(db, tenantId, adminId, title: "New",
            launchedAt: DateTime.UtcNow.AddDays(-2));

        // Each survey needs a 0–10 NPS question and at least one answer
        foreach (var s in new[] { surveyOld, surveyMiddle, surveyNew })
        {
            var npsQ = await SeedRatingQuestionAsync(db, tenantId, adminId, s.Id, min: 0, max: 10);
            var u    = await SeedUserAsync(db, tenantId, adminId);
            var (resp, _) = await SeedResponseAsync(db, tenantId, u.Id, s.Id);
            await SeedRatingAnswerAsync(db, tenantId, resp.Id, npsQ.Id, 10, adminId);
        }

        var trend = await sut.GetNpsTrendAsync(
            new List<Guid> { surveyNew.Id, surveyOld.Id, surveyMiddle.Id });

        Assert.Equal(3, trend.DataPoints.Count);
        // Must be in chronological order (oldest first)
        Assert.True(trend.DataPoints[0].LaunchedAt <= trend.DataPoints[1].LaunchedAt);
        Assert.True(trend.DataPoints[1].LaunchedAt <= trend.DataPoints[2].LaunchedAt);
    }

    // -- GetDepartmentBreakdownAsync ----------------------------------------

    [Fact]
    public async Task GetDepartmentBreakdownAsync_RatingQuestion_Returns_AvgPerDept()
    {
        var (db, tenantId, adminId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Admin);
        var currentUser = FakeCurrentUserAccessor.AsAdmin(adminId, tenantId);
        var sut = CreateService(db, currentUser);
        var survey = await SeedSurveyAsync(db, tenantId, adminId, totalInvited: 4, totalResponded: 4);
        var q = await SeedRatingQuestionAsync(db, tenantId, adminId, survey.Id);

        // Engineering: ratings 4, 5 → avg 4.5
        var engUser1 = await SeedUserAsync(db, tenantId, adminId, dept: "Engineering", name: "Eng1");
        var engUser2 = await SeedUserAsync(db, tenantId, adminId, dept: "Engineering", name: "Eng2");
        // HR: rating 3 → avg 3.0
        var hrUser = await SeedUserAsync(db, tenantId, adminId, dept: "HR", name: "HR1");

        var (r1, _) = await SeedResponseAsync(db, tenantId, engUser1.Id, survey.Id);
        await SeedRatingAnswerAsync(db, tenantId, r1.Id, q.Id, 4, adminId);
        var (r2, _) = await SeedResponseAsync(db, tenantId, engUser2.Id, survey.Id);
        await SeedRatingAnswerAsync(db, tenantId, r2.Id, q.Id, 5, adminId);
        var (r3, _) = await SeedResponseAsync(db, tenantId, hrUser.Id, survey.Id);
        await SeedRatingAnswerAsync(db, tenantId, r3.Id, q.Id, 3, adminId);

        var breakdown = await sut.GetDepartmentBreakdownAsync(survey.Id, q.Id);

        Assert.Equal(q.Id, breakdown.QuestionId);
        var engRow = breakdown.Rows.FirstOrDefault(r => r.Department == "Engineering");
        var hrRow  = breakdown.Rows.FirstOrDefault(r => r.Department == "HR");

        Assert.NotNull(engRow);
        Assert.NotNull(hrRow);
        Assert.Equal(4.5, engRow!.AverageScore, precision: 1);
        Assert.Equal(3.0, hrRow!.AverageScore,  precision: 1);
    }
}
