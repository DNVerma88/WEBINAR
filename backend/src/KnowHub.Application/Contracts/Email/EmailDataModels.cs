namespace KnowHub.Application.Contracts.Email;

public record SessionReminderEmailData(
    string RecipientName,
    string RecipientEmail,
    Guid RecipientUserId,
    Guid TenantId,
    string SessionTitle,
    DateTime ScheduledAt,
    string? Location,
    string? MeetingLink,
    string SpeakerName
);

public record BadgeAwardEmailData(
    string RecipientName,
    string RecipientEmail,
    Guid RecipientUserId,
    Guid TenantId,
    string BadgeName,
    string BadgeCategory,
    string BadgeDescription,
    int XpGained
);

public record MentorPairingEmailData(
    string RecipientName,
    string RecipientEmail,
    Guid RecipientUserId,
    Guid TenantId,
    string MentorName,
    string MenteeName,
    DateTime PairingDate,
    string? FocusAreas
);

public record SessionApprovalEmailData(
    string RecipientName,
    string RecipientEmail,
    Guid RecipientUserId,
    Guid TenantId,
    string ProposalTitle,
    bool IsApproved,
    string? ReviewerComments
);

public record KnowledgeRequestClaimedEmailData(
    string RecipientName,
    string RecipientEmail,
    Guid RecipientUserId,
    Guid TenantId,
    string RequestTitle,
    string ClaimerName,
    DateTime ClaimedAt
);

public record SurveyInvitationEmailData(
    string RecipientEmail,
    string RecipientName,
    string SurveyTitle,
    string? WelcomeMessage,
    string SurveyUrl,
    DateTime ExpiresAt
);
