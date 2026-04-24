namespace KnowHub.Application.Contracts.Integrations;

public interface ISlackNotificationService
{
    Task SendSessionAnnouncementAsync(Guid tenantId, string sessionTitle, string sessionUrl, string speakerName, DateTime scheduledAt, CancellationToken cancellationToken);
    Task SendProposalApprovalAsync(Guid tenantId, string proposalTitle, bool isApproved, CancellationToken cancellationToken);
}
