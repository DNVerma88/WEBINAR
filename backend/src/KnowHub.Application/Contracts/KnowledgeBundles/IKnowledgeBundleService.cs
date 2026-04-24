using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface IKnowledgeBundleService
{
    Task<PagedResult<KnowledgeBundleDto>> GetBundlesAsync(GetBundlesRequest request, CancellationToken cancellationToken);
    Task<KnowledgeBundleDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<KnowledgeBundleDto> CreateAsync(CreateKnowledgeBundleRequest request, CancellationToken cancellationToken);
    Task<KnowledgeBundleDto> UpdateAsync(Guid id, UpdateKnowledgeBundleRequest request, CancellationToken cancellationToken);
    Task AddItemAsync(Guid bundleId, AddBundleItemRequest request, CancellationToken cancellationToken);
    Task RemoveItemAsync(Guid bundleId, Guid assetId, CancellationToken cancellationToken);
}
