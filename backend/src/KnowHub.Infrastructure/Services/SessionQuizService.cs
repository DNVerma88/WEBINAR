using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace KnowHub.Infrastructure.Services;

public class SessionQuizService : ISessionQuizService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IXpService _xpService;
    private readonly IStreakService _streakService;

    public SessionQuizService(
        KnowHubDbContext db,
        ICurrentUserAccessor currentUser,
        IXpService xpService,
        IStreakService streakService)
    {
        _db = db;
        _currentUser = currentUser;
        _xpService = xpService;
        _streakService = streakService;
    }

    public async Task<SessionQuizDto> GetQuizBySessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var quiz = await _db.SessionQuizzes
            .Include(q => q.Questions.OrderBy(qq => qq.OrderSequence))
            .Where(q => q.SessionId == sessionId && q.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (quiz is null) throw new NotFoundException("SessionQuiz for session", sessionId);
        return MapToDto(quiz);
    }

    public async Task<SessionQuizDto> CreateQuizAsync(
        Guid sessionId, CreateQuizRequest request, CancellationToken cancellationToken)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (session is null) throw new NotFoundException("Session", sessionId);

        if (!_currentUser.IsAdminOrAbove && session.SpeakerId != _currentUser.UserId)
            throw new ForbiddenException("Only the session speaker or an Admin may create a quiz.");

        var exists = await _db.SessionQuizzes
            .AnyAsync(q => q.SessionId == sessionId && q.TenantId == _currentUser.TenantId, cancellationToken);
        if (exists) throw new ConflictException("A quiz already exists for this session.");

        var quiz = new SessionQuiz
        {
            TenantId = _currentUser.TenantId,
            SessionId = sessionId,
            Title = request.Title,
            Description = request.Description,
            PassingThresholdPercent = request.PassingThresholdPercent,
            AllowRetry = request.AllowRetry,
            MaxAttempts = request.MaxAttempts,
            IsActive = true,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        quiz.Questions = request.Questions.Select(q => new QuizQuestion
        {
            TenantId = _currentUser.TenantId,
            QuizId = quiz.Id,
            QuestionText = q.QuestionText,
            QuestionType = q.QuestionType,
            Options = q.Options != null ? JsonSerializer.Serialize(q.Options) : null,
            CorrectAnswer = q.CorrectAnswer,
            OrderSequence = q.OrderSequence,
            Points = q.Points,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        }).ToList();

        _db.SessionQuizzes.Add(quiz);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(quiz);
    }

    public async Task<QuizAttemptResultDto> SubmitAttemptAsync(
        Guid sessionId, SubmitQuizAttemptRequest request, CancellationToken cancellationToken)
    {
        var attended = await _db.SessionRegistrations
            .AnyAsync(r => r.SessionId == sessionId
                && r.ParticipantId == _currentUser.UserId
                && r.TenantId == _currentUser.TenantId
                && r.Status == RegistrationStatus.Attended, cancellationToken);

        if (!attended)
        {
            var isAdminOrSpeaker = _currentUser.IsAdminOrAbove
                || await _db.Sessions.AnyAsync(
                    s => s.Id == sessionId && s.SpeakerId == _currentUser.UserId && s.TenantId == _currentUser.TenantId,
                    cancellationToken);
            if (!isAdminOrSpeaker)
                throw new ForbiddenException("You must have attended this session to take the quiz.");
        }

        var quiz = await _db.SessionQuizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.SessionId == sessionId
                && q.TenantId == _currentUser.TenantId
                && q.IsActive, cancellationToken);

        if (quiz is null) throw new NotFoundException("Active SessionQuiz for session", sessionId);

        var attemptCount = await _db.UserQuizAttempts
            .CountAsync(a => a.QuizId == quiz.Id
                && a.UserId == _currentUser.UserId
                && a.TenantId == _currentUser.TenantId, cancellationToken);

        if (attemptCount >= quiz.MaxAttempts)
            throw new ConflictException($"Maximum of {quiz.MaxAttempts} attempts allowed.");

        var (score, isPassed) = GradeAttempt(quiz, request.Answers);
        var isFirstPass = isPassed == true && attemptCount == 0;

        var attempt = new UserQuizAttempt
        {
            TenantId = _currentUser.TenantId,
            QuizId = quiz.Id,
            UserId = _currentUser.UserId,
            AttemptNumber = attemptCount + 1,
            Answers = JsonSerializer.Serialize(request.Answers),
            Score = score,
            IsPassed = isPassed,
            SubmittedAt = DateTime.UtcNow,
            GradedAt = HasAutoGradableOnly(quiz) ? DateTime.UtcNow : null,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.UserQuizAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);

        if (isFirstPass)
        {
            await _xpService.AwardXpAsync(new AwardXpRequest
            {
                UserId = _currentUser.UserId,
                TenantId = _currentUser.TenantId,
                EventType = XpEventType.CompleteQuiz,
                RelatedEntityType = "SessionQuiz",
                RelatedEntityId = quiz.Id
            }, cancellationToken);

            await _streakService.UpdateStreakAsync(_currentUser.UserId, _currentUser.TenantId, cancellationToken);
        }

        return new QuizAttemptResultDto
        {
            AttemptNumber = attempt.AttemptNumber,
            Score = attempt.Score,
            IsPassed = attempt.IsPassed,
            SubmittedAt = attempt.SubmittedAt,
            XpAwarded = isFirstPass
        };
    }

    public async Task<List<UserQuizAttemptDto>> GetMyAttemptsAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var quiz = await _db.SessionQuizzes
            .Where(q => q.SessionId == sessionId && q.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (quiz is null) throw new NotFoundException("SessionQuiz for session", sessionId);

        var attemptsQuery = _db.UserQuizAttempts
            .Where(a => a.QuizId == quiz.Id && a.TenantId == _currentUser.TenantId);

        if (!_currentUser.IsAdminOrAbove)
            attemptsQuery = attemptsQuery.Where(a => a.UserId == _currentUser.UserId);

        return await attemptsQuery
            .OrderBy(a => a.AttemptNumber)
            .Select(a => new UserQuizAttemptDto
            {
                Id = a.Id,
                AttemptNumber = a.AttemptNumber,
                Score = a.Score,
                IsPassed = a.IsPassed,
                SubmittedAt = a.SubmittedAt,
                GradedAt = a.GradedAt
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private static (decimal? score, bool? isPassed) GradeAttempt(
        SessionQuiz quiz, List<QuizAnswerRequest> answers)
    {
        if (!HasAutoGradableOnly(quiz)) return (null, null);

        var totalPoints = quiz.Questions.Sum(q => q.Points);
        if (totalPoints == 0) return (0, false);

        var earnedPoints = 0;
        foreach (var question in quiz.Questions.Where(q =>
            q.QuestionType is QuizQuestionType.MCQ or QuizQuestionType.TrueFalse))
        {
            var userAnswer = answers.FirstOrDefault(a => a.QuestionId == question.Id);
            if (userAnswer is null) continue;
            if (string.Equals(userAnswer.Answer, question.CorrectAnswer, StringComparison.OrdinalIgnoreCase))
                earnedPoints += question.Points;
        }

        var score = Math.Round((decimal)earnedPoints / totalPoints * 100, 2);
        var isPassed = score >= quiz.PassingThresholdPercent;
        return (score, isPassed);
    }

    private static bool HasAutoGradableOnly(SessionQuiz quiz)
        => quiz.Questions.All(q => q.QuestionType != QuizQuestionType.ShortText);

    public async Task<SessionQuizDto> UpdateQuizAsync(
        Guid sessionId, UpdateQuizRequest request, CancellationToken cancellationToken)
    {
        var quiz = await _db.SessionQuizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.SessionId == sessionId && q.TenantId == _currentUser.TenantId, cancellationToken);

        if (quiz is null) throw new NotFoundException("SessionQuiz for session", sessionId);

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == _currentUser.TenantId, cancellationToken);
        if (!_currentUser.IsAdminOrAbove && session?.SpeakerId != _currentUser.UserId)
            throw new ForbiddenException("Only the session speaker or an Admin may update a quiz.");

        quiz.Title = request.Title;
        quiz.Description = request.Description;
        quiz.PassingThresholdPercent = request.PassingThresholdPercent;
        quiz.AllowRetry = request.AllowRetry;
        quiz.MaxAttempts = request.MaxAttempts;
        quiz.IsActive = request.IsActive;
        quiz.ModifiedBy = _currentUser.UserId;

        // Replace questions: explicitly remove old via DbSet, clear collection,
        // then add new ones directly — avoids EF relationship fixup issuing spurious UPDATEs
        var oldQuestions = quiz.Questions.ToList();
        _db.QuizQuestions.RemoveRange(oldQuestions);
        quiz.Questions.Clear();

        var newQuestions = request.Questions.Select(q => new QuizQuestion
        {
            TenantId = _currentUser.TenantId,
            QuizId = quiz.Id,
            QuestionText = q.QuestionText,
            QuestionType = q.QuestionType,
            Options = q.Options != null ? JsonSerializer.Serialize(q.Options) : null,
            CorrectAnswer = q.CorrectAnswer,
            OrderSequence = q.OrderSequence,
            Points = q.Points,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        }).ToList();

        _db.QuizQuestions.AddRange(newQuestions);

        await _db.SaveChangesAsync(cancellationToken);

        // Refresh from DB so MapToDto has the persisted questions
        quiz = await _db.SessionQuizzes
            .Include(q => q.Questions.OrderBy(qq => qq.OrderSequence))
            .FirstAsync(q => q.Id == quiz.Id, cancellationToken);

        return MapToDto(quiz);
    }

    private static SessionQuizDto MapToDto(SessionQuiz quiz) => new()
    {
        Id = quiz.Id,
        SessionId = quiz.SessionId,
        Title = quiz.Title,
        Description = quiz.Description,
        PassingThresholdPercent = quiz.PassingThresholdPercent,
        AllowRetry = quiz.AllowRetry,
        MaxAttempts = quiz.MaxAttempts,
        IsActive = quiz.IsActive,
        Questions = quiz.Questions.Select(q => new QuizQuestionDto
        {
            Id = q.Id,
            QuestionText = q.QuestionText,
            QuestionType = q.QuestionType,
            Options = q.Options != null
                ? JsonSerializer.Deserialize<List<string>>(q.Options)
                : null,
            OrderSequence = q.OrderSequence,
            Points = q.Points
        }).ToList()
    };
}
