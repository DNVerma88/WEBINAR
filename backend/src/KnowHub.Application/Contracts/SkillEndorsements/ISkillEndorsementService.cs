using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface ISkillEndorsementService
{
    Task<SkillEndorsementDto> EndorseAsync(Guid sessionId, EndorseSkillRequest request, CancellationToken cancellationToken);
    Task<PagedResult<SkillEndorsementDto>> GetEndorsementsForUserAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}
