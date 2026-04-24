using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface IKnowledgeAssetService
{
    Task<PagedResult<KnowledgeAssetDto>> GetAssetsAsync(GetAssetsRequest request, CancellationToken cancellationToken);
    Task<KnowledgeAssetDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<KnowledgeAssetDto> CreateAsync(CreateKnowledgeAssetRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
