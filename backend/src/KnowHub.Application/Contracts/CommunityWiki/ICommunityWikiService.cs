namespace KnowHub.Application.Contracts;

public interface ICommunityWikiService
{
    Task<List<WikiPageDto>> GetPagesAsync(Guid communityId, CancellationToken cancellationToken);
    Task<WikiPageDto> GetPageAsync(Guid communityId, Guid pageId, CancellationToken cancellationToken);
    Task<WikiPageDto> CreatePageAsync(Guid communityId, CreateWikiPageRequest request, CancellationToken cancellationToken);
    Task<WikiPageDto> UpdatePageAsync(Guid communityId, Guid pageId, UpdateWikiPageRequest request, CancellationToken cancellationToken);
    Task DeletePageAsync(Guid communityId, Guid pageId, CancellationToken cancellationToken);
}
