using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface ISessionService
{
    Task<PagedResult<SessionDto>> GetSessionsAsync(GetSessionsRequest request, CancellationToken cancellationToken);
    Task<SessionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<SessionDto> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken);
    Task<SessionDto> UpdateAsync(Guid id, UpdateSessionRequest request, CancellationToken cancellationToken);
    Task<SessionDto> CancelAsync(Guid id, CancellationToken cancellationToken);
    Task<SessionDto> CompleteAsync(Guid id, CancellationToken cancellationToken);
    Task<SessionRegistrationDto> RegisterAsync(Guid sessionId, CancellationToken cancellationToken);
    Task CancelRegistrationAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionMaterialDto>> GetMaterialsAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<SessionMaterialDto> AddMaterialAsync(Guid sessionId, AddSessionMaterialRequest request, CancellationToken cancellationToken);
    Task<SessionRatingSummaryDto> GetRatingsSummaryAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<SessionRatingDto> SubmitRatingAsync(Guid sessionId, SubmitSessionRatingRequest request, CancellationToken cancellationToken);
}
