using FluentValidation;
using KnowHub.Application.Models.Surveys;

namespace KnowHub.Application.Validators.Surveys;

public class UpdateSurveyRequestValidator : AbstractValidator<UpdateSurveyRequest>
{
    public UpdateSurveyRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.WelcomeMessage)
            .MaximumLength(1000).WithMessage("Welcome message must not exceed 1000 characters.")
            .When(x => x.WelcomeMessage is not null);

        RuleFor(x => x.ThankYouMessage)
            .MaximumLength(1000).WithMessage("Thank you message must not exceed 1000 characters.")
            .When(x => x.ThankYouMessage is not null);

        RuleFor(x => x.EndsAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Survey end date must be in the future.")
            .When(x => x.EndsAt.HasValue);

        RuleFor(x => x.RecordVersion)
            .GreaterThan(0).WithMessage("RecordVersion must be a positive integer.");
    }
}
