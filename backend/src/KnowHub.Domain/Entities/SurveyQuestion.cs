using KnowHub.Domain.Enums;

namespace KnowHub.Domain.Entities;

public class SurveyQuestion : BaseEntity
{
    public Guid SurveyId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public SurveyQuestionType QuestionType { get; set; }
    /// <summary>
    /// For SingleChoice and MultipleChoice: JSON array of option strings.
    /// Null for Text, Rating, YesNo. Stored as JSONB.
    /// </summary>
    public string? OptionsJson { get; set; }
    public int MinRating { get; set; } = 1;
    public int MaxRating { get; set; } = 5;
    public bool IsRequired { get; set; } = true;
    public int OrderSequence { get; set; }

    // Navigation
    public Survey Survey { get; set; } = null!;
    public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}
