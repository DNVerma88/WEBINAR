using FluentValidation;
using KnowHub.Application.Contracts.Assessment;

namespace KnowHub.Application.Validators.Assessment;

public class CreateAssessmentGroupRequestValidator : AbstractValidator<CreateAssessmentGroupRequest>
{
    public CreateAssessmentGroupRequestValidator()
    {
        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(200);

        RuleFor(x => x.GroupCode)
            .NotEmpty().WithMessage("Group code is required.")
            .MaximumLength(50)
            .Matches(@"^[A-Z0-9_\-]+$").WithMessage("Group code must contain only uppercase letters, digits, underscores, or hyphens.");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description is not null);

        RuleFor(x => x.PrimaryLeadUserId)
            .NotEmpty().WithMessage("Primary lead user is required.");

        RuleFor(x => x.AssessmentCategory)
            .MaximumLength(100).When(x => x.AssessmentCategory is not null);
    }
}

public class UpdateAssessmentGroupRequestValidator : AbstractValidator<UpdateAssessmentGroupRequest>
{
    public UpdateAssessmentGroupRequestValidator()
    {
        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description is not null);

        RuleFor(x => x.PrimaryLeadUserId)
            .NotEmpty().WithMessage("Primary lead user is required.");

        RuleFor(x => x.AssessmentCategory)
            .MaximumLength(100).When(x => x.AssessmentCategory is not null);

        RuleFor(x => x.RecordVersion)
            .GreaterThanOrEqualTo(0);
    }
}

public class CreateAssessmentPeriodRequestValidator : AbstractValidator<CreateAssessmentPeriodRequest>
{
    public CreateAssessmentPeriodRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Period name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Year)
            .InclusiveBetween(2020, 2100).WithMessage("Year must be between 2020 and 2100.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be on or after start date.");

        RuleFor(x => x.WeekNumber)
            .InclusiveBetween(1, 52).When(x => x.WeekNumber.HasValue)
            .WithMessage("Week number must be between 1 and 52.");
    }
}

public class CreateRatingScaleRequestValidator : AbstractValidator<CreateRatingScaleRequest>
{
    public CreateRatingScaleRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Rating code is required.")
            .MaximumLength(50);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Rating name is required.")
            .MaximumLength(100);

        RuleFor(x => x.NumericValue)
            .InclusiveBetween(0, 10).WithMessage("Numeric value must be between 0 and 10.");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public class CreateRubricRequestValidator : AbstractValidator<CreateRubricRequest>
{
    public CreateRubricRequestValidator()
    {
        RuleFor(x => x.DesignationCode)
            .NotEmpty().WithMessage("Designation code is required.")
            .MaximumLength(100);

        RuleFor(x => x.RatingScaleId)
            .NotEmpty().WithMessage("Rating scale is required.");

        RuleFor(x => x.BehaviorDescription)
            .NotEmpty().WithMessage("Behavior description is required.")
            .MaximumLength(2000);

        RuleFor(x => x.ProcessDescription)
            .NotEmpty().WithMessage("Process description is required.")
            .MaximumLength(2000);

        RuleFor(x => x.EvidenceDescription)
            .NotEmpty().WithMessage("Evidence description is required.")
            .MaximumLength(2000);

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective from date is required.");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom).When(x => x.EffectiveTo.HasValue)
            .WithMessage("Effective to must be after effective from.");
    }
}

public class SaveAssessmentDraftRequestValidator : AbstractValidator<SaveAssessmentDraftRequest>
{
    public SaveAssessmentDraftRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Employee is required.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group is required.");

        RuleFor(x => x.AssessmentPeriodId)
            .NotEmpty().WithMessage("Assessment period is required.");

        RuleFor(x => x.RatingScaleId)
            .NotEmpty().WithMessage("Rating scale is required.");
    }
}
