namespace KnowHub.Infrastructure.Email;

public class EmailConfiguration
{
    public SmtpEmailConfiguration SMTP { get; set; } = new();
}

/// <summary>
/// SMTP provider settings — mirrors the MailSettings pattern used in legacy apps:
/// Host / Port / DisplayName / Mail / Username / Password.
/// </summary>
public class SmtpEmailConfiguration
{
    public string Host        { get; set; } = string.Empty;
    public int    Port        { get; set; } = 587;
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>The SMTP sender address (maps to "Mail" in legacy MailSettings).</summary>
    public string Mail        { get; set; } = string.Empty;
    public string Username    { get; set; } = string.Empty;
    public string Password    { get; set; } = string.Empty;
    public bool   EnableSsl   { get; set; } = true;
}
