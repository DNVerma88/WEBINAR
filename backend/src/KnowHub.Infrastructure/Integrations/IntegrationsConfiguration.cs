namespace KnowHub.Infrastructure.Integrations;

public class IntegrationsConfiguration
{
    public TeamsConfiguration Teams { get; set; } = new();
    public SlackConfiguration Slack { get; set; } = new();
    public ZoomConfiguration Zoom { get; set; } = new();
    public OutlookCalendarConfiguration OutlookCalendar { get; set; } = new();
}

public class TeamsConfiguration
{
    public string IncomingWebhookUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public class SlackConfiguration
{
    public string BotToken { get; set; } = string.Empty;
    public string DefaultChannel { get; set; } = "#knowledge-hub";
    public bool Enabled { get; set; }
}

public class ZoomConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public class OutlookCalendarConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
