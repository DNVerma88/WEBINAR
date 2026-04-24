using KnowHub.Application.Models;
using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public interface ILearningPathService
{
    Task<PagedResult<LearningPathDto>> GetPathsAsync(GetLearningPathsRequest request, CancellationToken cancellationToken);
    Task<LearningPathDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<LearningPathDto> CreateAsync(CreateLearningPathRequest request, CancellationToken cancellationToken);
    Task<LearningPathDto> UpdateAsync(Guid id, UpdateLearningPathRequest request, CancellationToken cancellationToken);
    Task<LearningPathItemDto> AddItemAsync(Guid pathId, AddLearningPathItemRequest request, CancellationToken cancellationToken);
    Task RemoveItemAsync(Guid pathId, Guid itemId, CancellationToken cancellationToken);
    Task EnrolAsync(Guid pathId, CancellationToken cancellationToken);
    Task UnenrolAsync(Guid pathId, CancellationToken cancellationToken);
    Task<EnrolmentProgressDto> GetProgressAsync(Guid pathId, CancellationToken cancellationToken);
    Task<LearningPathCertificateDto> GetCertificateAsync(Guid pathId, CancellationToken cancellationToken);
    Task<List<UserEnrolmentDto>> GetUserEnrolmentsAsync(Guid userId, CancellationToken cancellationToken);
}
