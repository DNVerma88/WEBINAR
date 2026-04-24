namespace KnowHub.Domain.Enums;

public enum SurveyQuestionType
{
    Text           = 0, // Open-ended textarea
    SingleChoice   = 1, // Radio buttons — exactly one option selected (covers Likert scale)
    MultipleChoice = 2, // Checkboxes — one or more options selected
    Rating         = 3, // Numeric slider/stars; scale defined by MinRating/MaxRating (covers NPS 0–10)
    YesNo          = 4, // Boolean — rendered as two radio buttons "Yes" / "No"
}
