using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface ISessionChapterService
{
    Task<PagedResult<SessionChapterDto>> GetChaptersAsync(Guid sessionId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<SessionChapterDto> AddChapterAsync(Guid sessionId, AddChapterRequest request, CancellationToken cancellationToken);
    Task DeleteChapterAsync(Guid chapterId, CancellationToken cancellationToken);
}
