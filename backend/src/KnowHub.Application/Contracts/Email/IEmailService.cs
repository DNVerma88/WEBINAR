namespace KnowHub.Application.Contracts.Email;

public interface IEmailService
{
    // -- Generic API (single + bulk) ----------------------------------------
    /// <summary>Send a single email using the configured provider.</summary>
    Task SendAsync(SendEmailRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send the same email to multiple recipients.
    /// SES: concurrent sends (max 10 in parallel, respects rate limits).
    /// SMTP: sequential sends.
    /// </summary>
    Task SendBulkAsync(BulkEmailRequest request, CancellationToken cancellationToken = default);

    // -- Domain-specific emails ---------------------------------------------
    Task SendWeeklyDigestAsync(WeeklyDigestEmailData data, CancellationToken cancellationToken);
    Task SendSessionReminderAsync(SessionReminderEmailData data, CancellationToken cancellationToken);
    Task SendBadgeAwardAsync(BadgeAwardEmailData data, CancellationToken cancellationToken);
    Task SendMentorPairingAsync(MentorPairingEmailData data, CancellationToken cancellationToken);
    Task SendSessionApprovalNotificationAsync(SessionApprovalEmailData data, CancellationToken cancellationToken);
    Task SendKnowledgeRequestClaimedAsync(KnowledgeRequestClaimedEmailData data, CancellationToken cancellationToken);
    Task SendSurveyInvitationAsync(SurveyInvitationEmailData data, CancellationToken cancellationToken);
}
