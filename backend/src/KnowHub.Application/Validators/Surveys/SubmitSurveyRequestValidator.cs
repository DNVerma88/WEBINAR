using FluentValidation;
using KnowHub.Application.Models.Surveys;

namespace KnowHub.Application.Validators.Surveys;

public class SubmitSurveyRequestValidator : AbstractValidator<SubmitSurveyRequest>
{
    public SubmitSurveyRequestValidator()
    {
        RuleFor(x => x.Answers)
            .NotEmpty().WithMessage("At least one answer is required.");

        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.AnswerText)
                .MaximumLength(5000).WithMessage("Answer text must not exceed 5000 characters.")
                .When(a => a.AnswerText is not null);

            answer.RuleFor(a => a.AnswerOptions)
                .Must(opts => opts == null || opts.Count <= 20)
                .WithMessage("No more than 20 options can be selected.")
                .When(a => a.AnswerOptions is not null);
        });
    }
}
