using FluentValidation;
using KnowHub.Application.Contracts.Talent;

namespace KnowHub.Application.Validators.Talent;

public class SaveResumeProfileRequestValidator : AbstractValidator<SaveResumeProfileRequest>
{
    public SaveResumeProfileRequestValidator()
    {
        RuleFor(x => x.Template)
            .NotEmpty().WithMessage("Template is required.")
            .MaximumLength(50);

        RuleFor(x => x.PersonalInfo)
            .NotEmpty().WithMessage("Personal information is required.");

        RuleFor(x => x.Summary)
            .MaximumLength(2000).When(x => x.Summary is not null);

        RuleFor(x => x.WorkExperience)
            .NotEmpty().WithMessage("Work experience field is required (may be an empty JSON array).");

        RuleFor(x => x.Education)
            .NotEmpty().WithMessage("Education field is required (may be an empty JSON array).");

        RuleFor(x => x.Skills)
            .NotEmpty().WithMessage("Skills field is required (may be an empty JSON array).");
    }
}
