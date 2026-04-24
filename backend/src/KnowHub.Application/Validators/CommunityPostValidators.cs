using FluentValidation;
using KnowHub.Application.Contracts;

namespace KnowHub.Application.Validators;

public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{
    public CreatePostRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300);

        RuleFor(x => x.ContentMarkdown)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(200_000).WithMessage("Content must not exceed 200,000 characters.");

        RuleFor(x => x.CoverImageUrl)
            .Must(BeHttpUrl).WithMessage("Cover image must be an absolute http/https URL.")
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.CoverImageUrl));

        RuleFor(x => x.CanonicalUrl)
            .Must(BeHttpUrl).WithMessage("Canonical URL must be an absolute http/https URL.")
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.CanonicalUrl));

        RuleFor(x => x.TagSlugs)
            .Must(t => t.Count <= 4).WithMessage("A maximum of 4 tags is allowed.");

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Scheduled time must be in the future.")
            .When(x => x.ScheduledAt.HasValue);
    }

    private static bool BeHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}

public class UpdatePostRequestValidator : AbstractValidator<UpdatePostRequest>
{
    public UpdatePostRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300);

        RuleFor(x => x.ContentMarkdown)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(200_000);

        RuleFor(x => x.CoverImageUrl)
            .Must(BeHttpUrl).WithMessage("Cover image must be an absolute http/https URL.")
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.CoverImageUrl));

        RuleFor(x => x.CanonicalUrl)
            .Must(BeHttpUrl).WithMessage("Canonical URL must be an absolute http/https URL.")
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.CanonicalUrl));

        RuleFor(x => x.TagSlugs)
            .Must(t => t.Count <= 4).WithMessage("A maximum of 4 tags is allowed.");
    }

    private static bool BeHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}

public class AddCommentRequestValidator : AbstractValidator<AddCommentRequest>
{
    public AddCommentRequestValidator()
    {
        RuleFor(x => x.BodyMarkdown)
            .NotEmpty().WithMessage("Comment body is required.")
            .MaximumLength(10_000).WithMessage("Comment must not exceed 10,000 characters.");
    }
}
