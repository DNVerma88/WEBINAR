using FluentValidation;
using KnowHub.Application.Contracts.Talent;

namespace KnowHub.Application.Validators.Talent;

public class CreateScreeningJobRequestValidator : AbstractValidator<CreateScreeningJobRequest>
{
    public CreateScreeningJobRequestValidator()
    {
        RuleFor(x => x.JobTitle)
            .NotEmpty().WithMessage("Job title is required.")
            .MaximumLength(300);

        // At least one JD source must be provided
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.JdText) || x.JdFileReference is not null)
            .WithMessage("Either JD text or a JD file reference must be provided.");

        When(x => x.JdText is not null, () =>
        {
            RuleFor(x => x.JdText)
                .MaximumLength(100_000).WithMessage("JD text must not exceed 100 000 characters.");
        });

        When(x => x.JdFileReference is not null, () =>
        {
            RuleFor(x => x.JdFileReference!.ProviderType)
                .NotEmpty().WithMessage("Storage provider type is required.")
                .Must(t => t is "Local" or "AzureBlob" or "S3" or "OneDrive" or "SharePoint")
                .WithMessage("Invalid storage provider type.");

            RuleFor(x => x.JdFileReference!.FileName)
                .NotEmpty().WithMessage("File name is required.")
                .Must(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                        || f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Only PDF and DOCX files are supported.");
        });
    }
}
