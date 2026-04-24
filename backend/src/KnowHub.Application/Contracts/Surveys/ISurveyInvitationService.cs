using KnowHub.Application.Models;
using KnowHub.Application.Models.Surveys;

namespace KnowHub.Application.Contracts.Surveys;

public interface ISurveyInvitationService
{
    Task<PagedResult<SurveyInvitationDto>> GetInvitationsAsync(Guid surveyId, GetInvitationsRequest request, CancellationToken ct);
    Task ResendToUserAsync(Guid surveyId, Guid userId, CancellationToken ct);
    Task ResendBulkAsync(Guid surveyId, ResendInvitationsRequest request, CancellationToken ct);
    Task ResendAllPendingAsync(Guid surveyId, CancellationToken ct);

    /// <summary>Called by SurveyLaunchJob: loads active employees, creates invitations, and sends emails.</summary>
    Task CreateInvitationsAndSendAsync(Guid surveyId, CancellationToken ct);

    /// <summary>Called by SurveyTokenExpiryJob: bulk-marks Sent invitations whose ExpiresAt has passed as Expired.</summary>
    Task MarkExpiredAsync(CancellationToken ct);
}
