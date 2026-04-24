using FluentValidation;
using KnowHub.Application.Contracts;

namespace KnowHub.Application.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z0-9\s]+$").WithMessage("Tag name must contain only alphanumeric characters and spaces.");
    }
}

public class CreateCommunityRequestValidator : AbstractValidator<CreateCommunityRequest>
{
    public CreateCommunityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Community name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(1000).When(x => x.Description is not null);
    }
}

public class SubmitSessionRatingRequestValidator : AbstractValidator<SubmitSessionRatingRequest>
{
    public SubmitSessionRatingRequestValidator()
    {
        RuleFor(x => x.SessionScore)
            .InclusiveBetween(1, 5).WithMessage("Session score must be between 1 and 5.");

        RuleFor(x => x.SpeakerScore)
            .InclusiveBetween(1, 5).WithMessage("Speaker score must be between 1 and 5.");

        RuleFor(x => x.FeedbackText)
            .MaximumLength(2000).When(x => x.FeedbackText is not null);
    }
}

public class CreateKnowledgeRequestRequestValidator : AbstractValidator<CreateKnowledgeRequestRequest>
{
    public CreateKnowledgeRequestRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000);

        RuleFor(x => x.BountyXp)
            .GreaterThanOrEqualTo(0).WithMessage("Bounty XP cannot be negative.");
    }
}

// B26: validate that user-supplied profile photo URL uses HTTPS to prevent XSS via javascript: / data: URIs
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.ProfilePhotoUrl)
            .Must(url => string.IsNullOrEmpty(url) ||
                (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Profile photo URL must be a valid HTTPS URL.")
            .When(x => x.ProfilePhotoUrl is not null);
    }
}
