using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class SessionQuizDto
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int PassingThresholdPercent { get; init; }
    public bool AllowRetry { get; init; }
    public int MaxAttempts { get; init; }
    public bool IsActive { get; init; }
    public List<QuizQuestionDto> Questions { get; init; } = new();
}

public class QuizQuestionDto
{
    public Guid Id { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public QuizQuestionType QuestionType { get; init; }
    public List<string>? Options { get; init; }
    public int OrderSequence { get; init; }
    public int Points { get; init; }
}

public class QuizAttemptResultDto
{
    public int AttemptNumber { get; init; }
    public decimal? Score { get; init; }
    public bool? IsPassed { get; init; }
    public DateTime SubmittedAt { get; init; }
    public bool XpAwarded { get; init; }
}

public class UserQuizAttemptDto
{
    public Guid Id { get; init; }
    public int AttemptNumber { get; init; }
    public decimal? Score { get; init; }
    public bool? IsPassed { get; init; }
    public DateTime SubmittedAt { get; init; }
    public DateTime? GradedAt { get; init; }
}

public class CreateQuizRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PassingThresholdPercent { get; set; } = 70;
    public bool AllowRetry { get; set; } = true;
    public int MaxAttempts { get; set; } = 2;
    public List<CreateQuizQuestionRequest> Questions { get; set; } = new();
}

public class UpdateQuizRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PassingThresholdPercent { get; set; } = 70;
    public bool AllowRetry { get; set; } = true;
    public int MaxAttempts { get; set; } = 2;
    public bool IsActive { get; set; } = true;
    public List<CreateQuizQuestionRequest> Questions { get; set; } = new();
}

public class CreateQuizQuestionRequest
{
    public string QuestionText { get; set; } = string.Empty;
    public QuizQuestionType QuestionType { get; set; }
    public List<string>? Options { get; set; }
    public string? CorrectAnswer { get; set; }
    public int OrderSequence { get; set; }
    public int Points { get; set; } = 1;
}

public class SubmitQuizAttemptRequest
{
    public List<QuizAnswerRequest> Answers { get; set; } = new();
}

public class QuizAnswerRequest
{
    public Guid QuestionId { get; set; }
    public string Answer { get; set; } = string.Empty;
}
