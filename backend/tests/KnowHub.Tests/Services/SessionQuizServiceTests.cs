using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class SessionQuizServiceTests
{
    private sealed class NoOpXpService : IXpService
    {
        public Task<UserXpDto> GetUserXpAsync(Guid userId, CancellationToken ct)
            => Task.FromResult(new UserXpDto { UserId = userId });

        public Task AwardXpAsync(AwardXpRequest request, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class NoOpStreakService : IStreakService
    {
        public Task<UserStreakDto> GetStreakAsync(Guid userId, CancellationToken ct)
            => Task.FromResult(new UserStreakDto { UserId = userId });

        public Task UpdateStreakAsync(Guid userId, Guid tenantId, CancellationToken ct)
            => Task.CompletedTask;
    }

    private static SessionQuizService CreateService(
        KnowHub.Infrastructure.Persistence.KnowHubDbContext db,
        ICurrentUserAccessor currentUser)
        => new(db, currentUser, new NoOpXpService(), new NoOpStreakService());

    private static async Task<(KnowHub.Infrastructure.Persistence.KnowHubDbContext db, Guid tenantId, Guid speakerId, Guid sessionId)> SetupWithSessionAsync(bool currentUserIsSpeaker = true)
    {
        var (db, tenantId, userId) = await TestDbFactory.CreateWithSeedAsync(UserRole.Contributor);

        var speakerId = currentUserIsSpeaker ? userId : Guid.NewGuid();

        if (!currentUserIsSpeaker)
        {
            db.Users.Add(new User
            {
                Id = speakerId, TenantId = tenantId,
                FullName = "Speaker", Email = "speaker@knowhub.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = UserRole.Contributor, IsActive = true,
                CreatedBy = speakerId, ModifiedBy = speakerId
            });
        }

        var session = new Session
        {
            TenantId = tenantId,
            Title = "CloudNative Session",
            Description = "Cloud Native with K8s",
            SpeakerId = speakerId,
            Status = SessionStatus.Scheduled,
            ScheduledAt = DateTime.UtcNow.AddDays(7),
            DurationMinutes = 60,
            Format = SessionFormat.Webinar,
            CreatedBy = speakerId,
            ModifiedBy = speakerId
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();

        return (db, tenantId, userId, session.Id);
    }

    private static CreateQuizRequest MakeQuizRequest(string title = "Test Quiz") => new()
    {
        Title = title,
        Description = "Quiz description",
        PassingThresholdPercent = 60,
        AllowRetry = true,
        MaxAttempts = 3,
        Questions = new List<CreateQuizQuestionRequest>
        {
            new()
            {
                QuestionText = "Is the sky blue?",
                QuestionType = QuizQuestionType.TrueFalse,
                CorrectAnswer = "True",
                Points = 10,
                OrderSequence = 1
            }
        }
    };

    [Fact]
    public async Task GetQuizBySessionAsync_NoQuiz_ThrowsNotFoundException()
    {
        var (db, tenantId, userId, sessionId) = await SetupWithSessionAsync();
        var currentUser = FakeCurrentUserAccessor.AsContributor(userId, tenantId);
        var service = CreateService(db, currentUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetQuizBySessionAsync(sessionId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateQuizAsync_BySpeaker_CreatesQuizWithQuestions()
    {
        var (db, tenantId, userId, sessionId) = await SetupWithSessionAsync(currentUserIsSpeaker: true);
        var currentUser = FakeCurrentUserAccessor.AsContributor(userId, tenantId);
        var service = CreateService(db, currentUser);

        var result = await service.CreateQuizAsync(sessionId, MakeQuizRequest("K8s Knowledge Check"), CancellationToken.None);

        Assert.Equal("K8s Knowledge Check", result.Title);
        Assert.Single(result.Questions);
    }

    [Fact]
    public async Task CreateQuizAsync_AlreadyExists_ThrowsConflictException()
    {
        var (db, tenantId, userId, sessionId) = await SetupWithSessionAsync(currentUserIsSpeaker: true);
        var currentUser = FakeCurrentUserAccessor.AsContributor(userId, tenantId);
        var service = CreateService(db, currentUser);

        await service.CreateQuizAsync(sessionId, MakeQuizRequest(), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateQuizAsync(sessionId, MakeQuizRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task CreateQuizAsync_UserIsNotSpeakerOrAdmin_ThrowsForbiddenException()
    {
        var (db, tenantId, userId, sessionId) = await SetupWithSessionAsync(currentUserIsSpeaker: false);
        // userId is NOT the speaker — speaker is a different user
        var currentUser = FakeCurrentUserAccessor.AsContributor(userId, tenantId);
        var service = CreateService(db, currentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateQuizAsync(sessionId, MakeQuizRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task GetMyAttemptsAsync_NoAttempts_ReturnsEmptyList()
    {
        var (db, tenantId, userId, sessionId) = await SetupWithSessionAsync(currentUserIsSpeaker: true);
        var currentUser = FakeCurrentUserAccessor.AsContributor(userId, tenantId);
        var service = CreateService(db, currentUser);

        await service.CreateQuizAsync(sessionId, MakeQuizRequest(), CancellationToken.None);

        var attempts = await service.GetMyAttemptsAsync(sessionId, CancellationToken.None);

        Assert.Empty(attempts);
    }
}
