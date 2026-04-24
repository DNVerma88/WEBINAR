using KnowHub.Application.Contracts.Email;

namespace KnowHub.Tests.TestHelpers;

public class FakeEmailService : IEmailService
{
    public List<object> SentEmails { get; } = new();

    public Task SendWeeklyDigestAsync(WeeklyDigestEmailData data, CancellationToken cancellationToken)
    {
        SentEmails.Add(data);
        return Task.CompletedTask;
    }

    public Task SendSessionReminderAsync(SessionReminderEmailData data, CancellationToken cancellationToken)
    {
        SentEmails.Add(data);
        return Task.CompletedTask;
    }

    public Task SendBadgeAwardAsync(BadgeAwardEmailData data, CancellationToken cancellationToken)
    {
        SentEmails.Add(data);
        return Task.CompletedTask;
    }

    public Task SendMentorPairingAsync(MentorPairingEmailData data, CancellationToken cancellationToken)
    {
        SentEmails.Add(data);
        return Task.CompletedTask;
    }

    public Task SendSessionApprovalNotificationAsync(SessionApprovalEmailData data, CancellationToken cancellationToken)
    {
        SentEmails.Add(data);
        return Task.CompletedTask;
    }

    public Task SendKnowledgeRequestClaimedAsync(KnowledgeRequestClaimedEmailData data, CancellationToken cancellationToken)
    {
        SentEmails.Add(data);
        return Task.CompletedTask;
    }

    public Task SendSurveyInvitationAsync(SurveyInvitationEmailData data, CancellationToken cancellationToken)
    {
        SentEmails.Add(data);
        return Task.CompletedTask;
    }

    public Task SendAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        SentEmails.Add(request);
        return Task.CompletedTask;
    }

    public Task SendBulkAsync(BulkEmailRequest request, CancellationToken cancellationToken = default)
    {
        SentEmails.Add(request);
        return Task.CompletedTask;
    }

    public T? LastEmailOf<T>() => SentEmails.OfType<T>().LastOrDefault();
}
