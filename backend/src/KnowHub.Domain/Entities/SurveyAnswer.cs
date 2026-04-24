namespace KnowHub.Domain.Entities;

public class SurveyAnswer : BaseEntity
{
    public Guid ResponseId { get; set; }
    public Guid QuestionId { get; set; }
    /// <summary>Text answer for Text questions. Single selected label for YesNo/SingleChoice stored as text.</summary>
    public string? AnswerText { get; set; }
    /// <summary>JSONB array of selected option labels for MultipleChoice. Null for other types.</summary>
    public string? AnswerOptionsJson { get; set; }
    /// <summary>Numeric value for Rating questions. Null for other types.</summary>
    public int? RatingValue { get; set; }

    // Navigation
    public SurveyResponse Response { get; set; } = null!;
    public SurveyQuestion Question { get; set; } = null!;
}
