namespace KnowHub.Application.Contracts.Email;

/// <summary>
/// Generic request to send a single transactional email.
/// Use this for any one-off send that isn't covered by a domain-specific method.
/// </summary>
/// <param name="ToEmail">Recipient email address.</param>
/// <param name="Subject">Email subject line.</param>
/// <param name="HtmlBody">Full HTML body of the email.</param>
/// <param name="ToName">Optional display name for the recipient.</param>
/// <param name="PlainTextBody">Optional plain-text fallback body.</param>
/// <param name="ReplyToEmail">Optional reply-to address.</param>
public record SendEmailRequest(
    string  ToEmail,
    string  Subject,
    string  HtmlBody,
    string? ToName          = null,
    string? PlainTextBody   = null,
    string? ReplyToEmail    = null,
    string? EmailType       = null
);

/// <summary>
/// Generic request to send the same email to multiple recipients in one operation.
/// Bulk sends are processed concurrently (SES) or sequentially (SMTP).
/// </summary>
/// <param name="Recipients">List of recipients; each entry can carry an optional display name.</param>
/// <param name="Subject">Shared subject line for all recipients.</param>
/// <param name="HtmlBody">Shared HTML body for all recipients.</param>
/// <param name="PlainTextBody">Optional plain-text fallback.</param>
/// <param name="ReplyToEmail">Optional reply-to address.</param>
public record BulkEmailRequest(
    IReadOnlyList<EmailRecipient> Recipients,
    string  Subject,
    string  HtmlBody,
    string? PlainTextBody = null,
    string? ReplyToEmail  = null
);

/// <summary>A single recipient entry used in <see cref="BulkEmailRequest"/>.</summary>
/// <param name="Email">The recipient's email address.</param>
/// <param name="Name">Optional display name.</param>
public record EmailRecipient(string Email, string? Name = null);
