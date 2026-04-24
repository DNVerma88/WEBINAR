namespace KnowHub.Domain.Enums;

public enum ReportReason
{
    Spam = 0,
    Abuse = 1,
    Misinformation = 2,
    NSFW = 3,
    Copyright = 4,
}

public enum ReportStatus
{
    Open = 0,
    Resolved = 1,
    Dismissed = 2,
}
