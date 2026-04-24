using FluentValidation;
using KnowHub.Application.Contracts;

namespace KnowHub.Application.Validators;

public class CreateSessionProposalRequestValidator : AbstractValidator<CreateSessionProposalRequest>
{
    public CreateSessionProposalRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Topic)
            .NotEmpty().WithMessage("Topic is required.")
            .MaximumLength(300);

        RuleFor(x => x.Description)
            .MaximumLength(5000).When(x => x.Description is not null);

        RuleFor(x => x.EstimatedDurationMinutes)
            .InclusiveBetween(15, 480).WithMessage("Duration must be between 15 and 480 minutes.");

        RuleFor(x => x.ExpectedOutcomes)
            .MaximumLength(2000).When(x => x.ExpectedOutcomes is not null);

        RuleFor(x => x.TargetAudience)
            .MaximumLength(500).When(x => x.TargetAudience is not null);
    }
}
