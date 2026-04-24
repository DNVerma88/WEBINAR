namespace KnowHub.Application.Contracts;

public interface IAfterActionReviewService
{
    Task<AfterActionReviewDto> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<AfterActionReviewDto> CreateAsync(Guid sessionId, CreateAarRequest request, CancellationToken cancellationToken);
    Task<AfterActionReviewDto> UpdateAsync(Guid sessionId, UpdateAarRequest request, CancellationToken cancellationToken);
}
