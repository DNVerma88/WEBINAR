using System.Net;
using System.Net.Mail;
using KnowHub.Application.Contracts.Email;
using KnowHub.Domain.Entities;
using KnowHub.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowHub.Infrastructure.Email;

/// <summary>
/// SMTP email provider — mirrors the MailSettings pattern
/// (Host / Port / DisplayName / Mail / Username / Password / EnableSsl).
/// Inherits all domain-email methods and HTML builders from EmailServiceBase.
/// </summary>
public class SmtpEmailService : EmailServiceBase
{
    private readonly EmailConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly KnowHubDbContext _db;

    public SmtpEmailService(
        IOptions<EmailConfiguration> config,
        ILogger<SmtpEmailService> logger,
        KnowHubDbContext db)
    {
        _config = config.Value;
        _logger = logger;
        _db = db;
    }

    // -- Single send --------------------------------------------------------
    public override async Task SendAsync(
        SendEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var smtp = _config.SMTP;

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            Credentials    = new NetworkCredential(smtp.Username, smtp.Password),
            EnableSsl      = smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        using var message = new MailMessage
        {
            From       = new MailAddress(smtp.Mail, smtp.DisplayName),
            Subject    = request.Subject,
            Body       = request.HtmlBody,
            IsBodyHtml = true,
        };

        var displayName = request.ToName ?? string.Empty;
        message.To.Add(new MailAddress(request.ToEmail, displayName));

        if (!string.IsNullOrWhiteSpace(request.PlainTextBody))
            message.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(
                    request.PlainTextBody, null, "text/plain"));

        if (!string.IsNullOrWhiteSpace(request.ReplyToEmail))
            message.ReplyToList.Add(new MailAddress(request.ReplyToEmail));

        var log = new EmailLog
        {
            RecipientEmail = request.ToEmail,
            Subject        = request.Subject,
            EmailType      = request.EmailType ?? "Generic",
            CreatedBy      = Guid.Empty,
            ModifiedBy     = Guid.Empty,
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            log.Status = "Sent";
            log.SentAt = DateTime.UtcNow;
            _logger.LogInformation(
                "SMTP sent to {Email}, subject: {Subject}",
                request.ToEmail, request.Subject);
        }
        catch (Exception ex)
        {
            log.Status       = "Failed";
            log.ErrorMessage = ex.Message;
            _logger.LogError(ex, "SMTP failed to {Email}", request.ToEmail);
            throw;
        }
        finally
        {
            try
            {
                _db.EmailLogs.Add(log);
                await _db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "Failed to persist EmailLog for {Email}", request.ToEmail);
            }
        }
    }

    // -- Bulk send (sequential — avoids SMTP connection/rate-limit issues) ---
    public override async Task SendBulkAsync(
        BulkEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        foreach (var recipient in request.Recipients)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SendAsync(
                new SendEmailRequest(
                    recipient.Email,
                    request.Subject,
                    request.HtmlBody,
                    recipient.Name,
                    request.PlainTextBody,
                    request.ReplyToEmail),
                cancellationToken);
        }
    }
}
