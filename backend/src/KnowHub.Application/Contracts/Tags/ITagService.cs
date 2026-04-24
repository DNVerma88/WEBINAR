using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface ITagService
{
    Task<PagedResult<TagDto>> GetTagsAsync(GetTagsRequest request, CancellationToken cancellationToken);
    Task<TagDto> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<CommunityPostSummaryDto>> GetPostsByTagAsync(string tagSlug, GetPostsRequest request, CancellationToken cancellationToken);
    Task<bool> ToggleFollowTagAsync(string tagSlug, CancellationToken cancellationToken);
}
