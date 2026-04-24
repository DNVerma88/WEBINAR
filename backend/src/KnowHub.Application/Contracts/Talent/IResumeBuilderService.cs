namespace KnowHub.Application.Contracts.Talent;

public interface IResumeBuilderService
{
    Task<ResumeProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<ResumeProfileDto> SaveProfileAsync(SaveResumeProfileRequest request, CancellationToken ct = default);
    Task<Stream> GeneratePdfAsync(Guid userId, CancellationToken ct = default);
    Task<Stream> GenerateWordAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ResumeProfileAdminSummaryDto>> GetAllProfileSummariesAsync(CancellationToken ct = default);

    /// <summary>Returns a specific user's resume profile for admin viewing/editing. Target user must belong to the caller's tenant.</summary>
    Task<ResumeProfileDto?> GetProfileForAdminAsync(Guid targetUserId, CancellationToken ct = default);

    /// <summary>Creates or updates a specific user's resume profile on behalf of an admin. Target user must belong to the caller's tenant.</summary>
    Task<ResumeProfileDto> SaveProfileForAdminAsync(Guid targetUserId, SaveResumeProfileRequest request, CancellationToken ct = default);
}
