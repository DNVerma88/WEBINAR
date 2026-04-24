namespace KnowHub.Application.Contracts.Email;

public record WeeklyDigestEmailData(
    string RecipientName,
    string RecipientEmail,
    Guid RecipientUserId,
    Guid TenantId,
    int XpGainedThisWeek,
    int CurrentLeaderboardRank,
    int UnreadNotificationsCount,
    List<DigestSessionItem> TopSessions,
    List<DigestCommunityItem> CommunityHighlights
);

public record DigestSessionItem(
    Guid SessionId,
    string Title,
    string SpeakerName,
    DateTime ScheduledAt,
    int RegisteredCount
);

public record DigestCommunityItem(
    Guid CommunityId,
    string CommunityName,
    int NewWikiPages,
    int NewMembers
);
