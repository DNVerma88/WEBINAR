namespace KnowHub.Domain.Entities;

public class SessionQuiz : BaseEntity
{
    public Guid SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PassingThresholdPercent { get; set; } = 70;
    public bool AllowRetry { get; set; } = true;
    public int MaxAttempts { get; set; } = 2;
    public bool IsActive { get; set; } = true;

    public Session? Session { get; set; }
    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    public ICollection<UserQuizAttempt> Attempts { get; set; } = new List<UserQuizAttempt>();
}
