using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class QuizQuestion : BaseEntity
{
    public Guid QuizId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuizQuestionType QuestionType { get; set; }
    public string? Options { get; set; }
    public string? CorrectAnswer { get; set; }
    public int OrderSequence { get; set; }
    public int Points { get; set; } = 1;

    public SessionQuiz? Quiz { get; set; }
}
