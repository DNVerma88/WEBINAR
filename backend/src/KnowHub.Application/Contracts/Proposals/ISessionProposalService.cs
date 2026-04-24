using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface ISessionProposalService
{
    Task<PagedResult<SessionProposalDto>> GetProposalsAsync(GetSessionProposalsRequest request, CancellationToken cancellationToken);
    Task<SessionProposalDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<SessionProposalDto> CreateAsync(CreateSessionProposalRequest request, CancellationToken cancellationToken);
    Task<SessionProposalDto> UpdateAsync(Guid id, UpdateSessionProposalRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<SessionProposalDto> SubmitAsync(Guid id, CancellationToken cancellationToken);
    Task<SessionProposalDto> ApproveAsync(Guid id, ApproveProposalRequest request, CancellationToken cancellationToken);
    Task<SessionProposalDto> RejectAsync(Guid id, RejectProposalRequest request, CancellationToken cancellationToken);
    Task<SessionProposalDto> RequestRevisionAsync(Guid id, RequestRevisionRequest request, CancellationToken cancellationToken);
}
