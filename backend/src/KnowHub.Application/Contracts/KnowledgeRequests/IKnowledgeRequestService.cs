using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface IKnowledgeRequestService
{
    Task<PagedResult<KnowledgeRequestDto>> GetRequestsAsync(GetKnowledgeRequestsRequest request, CancellationToken cancellationToken);
    Task<KnowledgeRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<KnowledgeRequestDto> CreateAsync(CreateKnowledgeRequestRequest request, CancellationToken cancellationToken);
    Task<KnowledgeRequestDto> UpvoteAsync(Guid id, CancellationToken cancellationToken);
    Task<KnowledgeRequestDto> ClaimAsync(Guid id, CancellationToken cancellationToken);
    Task<KnowledgeRequestDto> CloseAsync(Guid id, CloseKnowledgeRequestRequest request, CancellationToken cancellationToken);
    Task<KnowledgeRequestDto> AddressAsync(Guid id, AddressKnowledgeRequestRequest request, CancellationToken cancellationToken);
}
