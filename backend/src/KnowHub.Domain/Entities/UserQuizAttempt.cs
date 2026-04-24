namespace KnowHub.Domain.Entities;

public class UserQuizAttempt : BaseEntity
{
    public Guid QuizId { get; set; }
    public Guid UserId { get; set; }
    public int AttemptNumber { get; set; }
    public string Answers { get; set; } = "[]";
    public decimal? Score { get; set; }
    public bool? IsPassed { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }

    public SessionQuiz? Quiz { get; set; }
    public User? User { get; set; }
}
