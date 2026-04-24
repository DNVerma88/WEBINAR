using FluentValidation;
using KnowHub.Application.Contracts;

namespace KnowHub.Application.Validators;

public class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
{
    private static readonly HashSet<string> AllowedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "teams.microsoft.com", "zoom.us", "meet.google.com", "webex.com"
    };

    public CreateSessionRequestValidator()
    {
        RuleFor(x => x.ProposalId)
            .NotEmpty().WithMessage("ProposalId is required.");

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Session must be scheduled in the future.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 480).WithMessage("Duration must be between 15 and 480 minutes.");

        RuleFor(x => x.MeetingLink)
            .NotEmpty().WithMessage("Meeting link is required.")
            .Must(BeAllowedMeetingUrl).WithMessage("Meeting link must be from an approved platform (Teams, Zoom, Google Meet, Webex).");

        RuleFor(x => x.ParticipantLimit)
            .GreaterThan(0).When(x => x.ParticipantLimit.HasValue)
            .WithMessage("Participant limit must be greater than 0.");
    }

    private static bool BeAllowedMeetingUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != "https") return false;
        return AllowedDomains.Any(d => uri.Host.EndsWith(d, StringComparison.OrdinalIgnoreCase));
    }
}
