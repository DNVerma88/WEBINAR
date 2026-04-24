using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface ICommunityService
{
    Task<PagedResult<CommunityDto>> GetCommunitiesAsync(GetCommunitiesRequest request, CancellationToken cancellationToken);
    Task<CommunityDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<CommunityDto> CreateAsync(CreateCommunityRequest request, CancellationToken cancellationToken);
    Task<CommunityDto> UpdateAsync(Guid id, UpdateCommunityRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task JoinAsync(Guid communityId, CancellationToken cancellationToken);
    Task LeaveAsync(Guid communityId, CancellationToken cancellationToken);
}
