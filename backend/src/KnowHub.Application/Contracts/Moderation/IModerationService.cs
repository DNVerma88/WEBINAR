using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts.Moderation;

public interface IModerationService
{
    Task<ContentFlagDto> FlagContentAsync(FlagContentRequest request, CancellationToken cancellationToken);
    Task<PagedResult<ContentFlagDto>> GetContentFlagsAsync(GetContentFlagsRequest request, CancellationToken cancellationToken);
    Task<ContentFlagDto> ReviewFlagAsync(Guid flagId, ReviewFlagRequest request, CancellationToken cancellationToken);
    Task<UserSuspensionDto> SuspendUserAsync(SuspendUserRequest request, CancellationToken cancellationToken);
    Task<UserSuspensionDto> LiftSuspensionAsync(Guid suspensionId, LiftSuspensionRequest request, CancellationToken cancellationToken);
    Task<PagedResult<UserSuspensionDto>> GetActiveSuspensionsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<List<UserSuspensionDto>> GetUserSuspensionsAsync(Guid userId, CancellationToken cancellationToken);
    Task BulkUpdateSessionStatusAsync(BulkSessionStatusRequest request, CancellationToken cancellationToken);
}
