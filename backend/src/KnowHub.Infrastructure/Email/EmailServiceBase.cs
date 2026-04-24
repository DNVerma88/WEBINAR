using System.Net;
using KnowHub.Application.Contracts.Email;

namespace KnowHub.Infrastructure.Email;

/// <summary>
/// Abstract base that owns all domain-specific email logic and HTML templates.
/// Concrete providers (AWS SES, SMTP) only implement <see cref="SendAsync"/> and
/// <see cref="SendBulkAsync"/>; every domain method delegates down to those.
/// </summary>
public abstract class EmailServiceBase : IEmailService
{
    // -- Abstract transport (override per provider) -------------------------
    public abstract Task SendAsync(SendEmailRequest request, CancellationToken cancellationToken = default);
    public abstract Task SendBulkAsync(BulkEmailRequest request, CancellationToken cancellationToken = default);

    // -- Domain email methods -----------------------------------------------
    public Task SendWeeklyDigestAsync(WeeklyDigestEmailData data, CancellationToken cancellationToken)
    {
        var subject = $"KnowHub Weekly Digest \u2014 {DateTime.UtcNow:MMMM d, yyyy}";
        return SendAsync(new SendEmailRequest(data.RecipientEmail, subject, BuildWeeklyDigestHtml(data), data.RecipientName), cancellationToken);
    }

    public Task SendSessionReminderAsync(SessionReminderEmailData data, CancellationToken cancellationToken)
    {
        var subject = $"Reminder: {data.SessionTitle} starts soon";
        return SendAsync(new SendEmailRequest(data.RecipientEmail, subject, BuildSessionReminderHtml(data), data.RecipientName), cancellationToken);
    }

    public Task SendBadgeAwardAsync(BadgeAwardEmailData data, CancellationToken cancellationToken)
    {
        var subject = $"You earned the \"{data.BadgeName}\" badge! \U0001f3c6";
        return SendAsync(new SendEmailRequest(data.RecipientEmail, subject, BuildBadgeAwardHtml(data), data.RecipientName), cancellationToken);
    }

    public Task SendMentorPairingAsync(MentorPairingEmailData data, CancellationToken cancellationToken)
    {
        var partner = data.RecipientName == data.MenteeName ? data.MentorName : data.MenteeName;
        var subject = $"You have been paired with {partner} for mentoring";
        return SendAsync(new SendEmailRequest(data.RecipientEmail, subject, BuildMentorPairingHtml(data), data.RecipientName), cancellationToken);
    }

    public Task SendSessionApprovalNotificationAsync(SessionApprovalEmailData data, CancellationToken cancellationToken)
    {
        var status  = data.IsApproved ? "Approved" : "Not Approved";
        var subject = $"Proposal Update: \"{data.ProposalTitle}\" \u2014 {status}";
        return SendAsync(new SendEmailRequest(data.RecipientEmail, subject, BuildSessionApprovalHtml(data), data.RecipientName), cancellationToken);
    }

    public Task SendKnowledgeRequestClaimedAsync(KnowledgeRequestClaimedEmailData data, CancellationToken cancellationToken)
    {
        var subject = $"Your knowledge request \"{data.RequestTitle}\" has been claimed";
        return SendAsync(new SendEmailRequest(data.RecipientEmail, subject, BuildKnowledgeRequestClaimedHtml(data), data.RecipientName), cancellationToken);
    }

    public Task SendSurveyInvitationAsync(SurveyInvitationEmailData data, CancellationToken cancellationToken)
        => SendAsync(BuildSurveyInvitationEmail(data), cancellationToken);

    private static SendEmailRequest BuildSurveyInvitationEmail(SurveyInvitationEmailData data)
    {
        // HTML-encode every user-controlled value to prevent malformed HTML that some SMTP
        // servers / email clients silently reject or garble.
        var encodedName    = WebUtility.HtmlEncode(data.RecipientName);
        var encodedTitle   = WebUtility.HtmlEncode(data.SurveyTitle);
        var encodedWelcome = data.WelcomeMessage is not null
            ? WebUtility.HtmlEncode(data.WelcomeMessage)
            : null;
        var encodedUrl     = WebUtility.HtmlEncode(data.SurveyUrl);  // encodes & in query strings

        var subject = $"You're invited: {data.SurveyTitle}";
        var body    = encodedWelcome
            ?? $"You have been invited to complete the survey: <strong>{encodedTitle}</strong>.";

        var html = $"""
            <html><body style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;">
              <h2 style="color:#1976d2;">Organizational Survey</h2>
              <p>Dear {encodedName},</p>
              <p>{body}</p>
              <p>Please complete the survey by <strong>{data.ExpiresAt:MMMM dd, yyyy}</strong>.</p>
              <div style="text-align:center;margin:30px 0;">
                <a href="{encodedUrl}"
                   style="background:#1976d2;color:#fff;padding:14px 28px;text-decoration:none;border-radius:4px;font-size:16px;">
                  Start Survey
                </a>
              </div>
              <p style="color:#666;font-size:13px;">
                If the button does not work, copy and paste this link into your browser:<br/>
                <a href="{encodedUrl}">{encodedUrl}</a>
              </p>
              <p style="color:#666;font-size:12px;">
                This link is unique to you and can only be used once. Do not share it with others.
              </p>
            </body></html>
            """;
        return new SendEmailRequest(data.RecipientEmail, subject, html, data.RecipientName);
    }

    // -- HTML template builders (shared by all providers) ------------------
    protected static string BuildWeeklyDigestHtml(WeeklyDigestEmailData data)
    {
        var sessions = string.Concat(data.TopSessions.Select(s =>
            $"<li><strong>{s.Title}</strong> by {s.SpeakerName} \u2014 {s.ScheduledAt:MMM d, HH:mm} UTC ({s.RegisteredCount} registered)</li>"));
        var communities = string.Concat(data.CommunityHighlights.Select(c =>
            $"<li><strong>{c.CommunityName}</strong>: {c.NewWikiPages} new wiki pages, {c.NewMembers} new members</li>"));

        return $@"<!DOCTYPE html><html><body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
<h1 style='color:#1976d2;'>KnowHub Weekly Digest</h1>
<p>Hi {data.RecipientName},</p>
<p>Here's what happened in KnowHub this week:</p>
<h2>Your Progress</h2>
<ul>
  <li>XP Gained This Week: <strong>+{data.XpGainedThisWeek} XP</strong></li>
  <li>Leaderboard Rank: <strong>#{data.CurrentLeaderboardRank}</strong></li>
  <li>Unread Notifications: <strong>{data.UnreadNotificationsCount}</strong></li>
</ul>
{(data.TopSessions.Any() ? $"<h2>Top Sessions This Week</h2><ul>{sessions}</ul>" : "")}
{(data.CommunityHighlights.Any() ? $"<h2>Community Highlights</h2><ul>{communities}</ul>" : "")}
<p style='color:#888;font-size:12px;margin-top:30px;'>You're receiving this because you're a KnowHub member.</p>
</body></html>";
    }

    protected static string BuildSessionReminderHtml(SessionReminderEmailData data)
    {
        var locationLine = data.MeetingLink is not null
            ? $"<li>Meeting Link: <a href='{data.MeetingLink}'>{data.MeetingLink}</a></li>"
            : data.Location is not null ? $"<li>Location: {data.Location}</li>" : "";

        return $@"<!DOCTYPE html><html><body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
<h1 style='color:#1976d2;'>Session Reminder</h1>
<p>Hi {data.RecipientName},</p>
<p>This is a reminder that the following session is coming up:</p>
<ul>
  <li>Session: <strong>{data.SessionTitle}</strong></li>
  <li>Speaker: {data.SpeakerName}</li>
  <li>Date &amp; Time: <strong>{data.ScheduledAt:MMMM d, yyyy HH:mm} UTC</strong></li>
  {locationLine}
</ul>
<p>We look forward to seeing you there!</p>
</body></html>";
    }

    protected static string BuildBadgeAwardHtml(BadgeAwardEmailData data) => $@"
<!DOCTYPE html><html><body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
<h1 style='color:#1976d2;'>\U0001f3c6 Badge Awarded!</h1>
<p>Hi {data.RecipientName},</p>
<p>Congratulations! You've earned the <strong>{data.BadgeName}</strong> badge.</p>
<ul>
  <li>Category: {data.BadgeCategory}</li>
  <li>Description: {data.BadgeDescription}</li>
  <li>XP Gained: <strong>+{data.XpGained} XP</strong></li>
</ul>
<p>Keep up the great work on KnowHub!</p>
</body></html>";

    protected static string BuildMentorPairingHtml(MentorPairingEmailData data) => $@"
<!DOCTYPE html><html><body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
<h1 style='color:#1976d2;'>Mentoring Pairing</h1>
<p>Hi {data.RecipientName},</p>
<p>You have been paired for a mentoring relationship:</p>
<ul>
  <li>Mentor: <strong>{data.MentorName}</strong></li>
  <li>Mentee: <strong>{data.MenteeName}</strong></li>
  <li>Paired On: {data.PairingDate:MMMM d, yyyy}</li>
  {(data.FocusAreas is not null ? $"<li>Focus Areas: {data.FocusAreas}</li>" : "")}
</ul>
<p>Log in to KnowHub to connect and set your goals.</p>
</body></html>";

    protected static string BuildSessionApprovalHtml(SessionApprovalEmailData data)
    {
        var statusColour = data.IsApproved ? "#4caf50" : "#f44336";
        var statusText   = data.IsApproved ? "Approved \u2713" : "Not Approved";
        return $@"<!DOCTYPE html><html><body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
<h1 style='color:{statusColour};'>Proposal {statusText}</h1>
<p>Hi {data.RecipientName},</p>
<p>Your session proposal <strong>{data.ProposalTitle}</strong> has been reviewed.</p>
<p>Decision: <strong style='color:{statusColour};'>{statusText}</strong></p>
{(data.ReviewerComments is not null ? $"<p>Reviewer Comments: {data.ReviewerComments}</p>" : "")}
<p>Log in to KnowHub to view the full details.</p>
</body></html>";
    }

    protected static string BuildKnowledgeRequestClaimedHtml(KnowledgeRequestClaimedEmailData data) => $@"
<!DOCTYPE html><html><body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
<h1 style='color:#1976d2;'>Knowledge Request Claimed</h1>
<p>Hi {data.RecipientName},</p>
<p>Your knowledge request <strong>{data.RequestTitle}</strong> has been claimed.</p>
<ul>
  <li>Claimed By: <strong>{data.ClaimerName}</strong></li>
  <li>Claimed On: {data.ClaimedAt:MMMM d, yyyy}</li>
</ul>
<p>Log in to KnowHub to track progress.</p>
</body></html>";
}
