using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface ISpeakerService
{
    Task<PagedResult<SpeakerDto>> GetSpeakersAsync(GetSpeakersRequest request, CancellationToken cancellationToken);
    Task<SpeakerDetailDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
}
