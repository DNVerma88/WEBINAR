using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken);
    Task<UserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserDto> AdminUpdateUserAsync(Guid id, AdminUpdateUserRequest request, CancellationToken cancellationToken);
    Task DeactivateUserAsync(Guid id, CancellationToken cancellationToken);
    Task FollowUserAsync(Guid targetUserId, CancellationToken cancellationToken);
    Task UnfollowUserAsync(Guid targetUserId, CancellationToken cancellationToken);
    Task<ContributorProfileDto> GetContributorProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<ContributorProfileDto> UpdateContributorProfileAsync(Guid userId, UpdateContributorProfileRequest request, CancellationToken cancellationToken);
}
