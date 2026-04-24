using FluentValidation;
using KnowHub.Application.Models.Surveys;
using KnowHub.Domain.Enums;

namespace KnowHub.Application.Validators.Surveys;

public class AddSurveyQuestionRequestValidator : AbstractValidator<AddSurveyQuestionRequest>
{
    public AddSurveyQuestionRequestValidator()
    {
        RuleFor(x => x.QuestionText)
            .NotEmpty().WithMessage("Question text is required.")
            .MaximumLength(1000).WithMessage("Question text must not exceed 1000 characters.");

        // Options required for choice types
        RuleFor(x => x.Options)
            .NotNull().WithMessage("Options are required for this question type.")
            .Must(opts => opts != null && opts.Count > 1).WithMessage("At least 2 options are required.")
            .Must(opts => opts == null || opts.Count <= 20).WithMessage("No more than 20 options are allowed.")
            .When(x => x.QuestionType is SurveyQuestionType.SingleChoice or SurveyQuestionType.MultipleChoice);

        // Options must be null for other types
        RuleFor(x => x.Options)
            .Null().WithMessage("Options must not be provided for this question type.")
            .When(x => x.QuestionType is SurveyQuestionType.Text or SurveyQuestionType.Rating or SurveyQuestionType.YesNo);

        // Each option not empty, max 200 chars, no duplicates
        RuleForEach(x => x.Options)
            .NotEmpty().WithMessage("Option label must not be empty.")
            .MaximumLength(200).WithMessage("Option label must not exceed 200 characters.")
            .When(x => x.Options is not null);

        RuleFor(x => x.Options)
            .Must(opts => opts == null || opts.Select(o => o.Trim().ToLowerInvariant()).Distinct().Count() == opts.Count)
            .WithMessage("Options must not contain duplicate labels.")
            .When(x => x.Options is not null);

        // Rating range
        RuleFor(x => x.MinRating)
            .InclusiveBetween(0, 9).WithMessage("MinRating must be between 0 and 9.")
            .When(x => x.QuestionType == SurveyQuestionType.Rating);

        RuleFor(x => x.MaxRating)
            .InclusiveBetween(1, 10).WithMessage("MaxRating must be between 1 and 10.")
            .Must((req, max) => max > req.MinRating).WithMessage("MaxRating must be greater than MinRating.")
            .When(x => x.QuestionType == SurveyQuestionType.Rating);
    }
}
