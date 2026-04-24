using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface IMentoringService
{
    Task<MentorMenteeDto> RequestMentorAsync(RequestMentorRequest request, CancellationToken cancellationToken);
    // B22: expose pagination parameters so callers can page through large result sets
    Task<PagedResult<MentorMenteeDto>> GetPairingsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<MentorMenteeDto> AcceptAsync(Guid requestId, CancellationToken cancellationToken);
    Task<MentorMenteeDto> DeclineAsync(Guid requestId, CancellationToken cancellationToken);
}
