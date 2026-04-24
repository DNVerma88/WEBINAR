using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Talent;
using KnowHub.Domain.Entities.Talent;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services.Talent;

public sealed class ResumeBuilderService : IResumeBuilderService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ResumeGenerator _generator;

    public ResumeBuilderService(KnowHubDbContext db, ICurrentUserAccessor currentUser, ResumeGenerator generator)
    {
        _db = db;
        _currentUser = currentUser;
        _generator = generator;
    }

    public async Task<ResumeProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await _db.ResumeProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == _currentUser.TenantId, ct);

        return profile is null ? null : MapToDto(profile);
    }

    public async Task<ResumeProfileDto> SaveProfileAsync(SaveResumeProfileRequest request, CancellationToken ct = default)
    {
        var profile = await _db.ResumeProfiles
            .FirstOrDefaultAsync(p => p.UserId == _currentUser.UserId && p.TenantId == _currentUser.TenantId, ct);

        if (profile is null)
        {
            profile = new ResumeProfile
            {
                Id = Guid.NewGuid(),
                TenantId = _currentUser.TenantId,
                UserId = _currentUser.UserId,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            _db.ResumeProfiles.Add(profile);
        }

        profile.Template = request.Template;
        profile.PersonalInfo = request.PersonalInfo;
        profile.Summary = request.Summary;
        profile.WorkExperience = request.WorkExperience;
        profile.Education = request.Education;
        profile.Skills = request.Skills;
        profile.Certifications = request.Certifications;
        profile.Projects = request.Projects;
        profile.Languages = request.Languages;
        profile.Publications = request.Publications;
        profile.Achievements = request.Achievements;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapToDto(profile);
    }

    public async Task<Stream> GeneratePdfAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await _db.ResumeProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("ResumeProfile", userId);

        var bytes = _generator.GeneratePdf(MapToDto(profile));
        return new MemoryStream(bytes);
    }

    public async Task<Stream> GenerateWordAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await _db.ResumeProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("ResumeProfile", userId);

        var bytes = _generator.GenerateWord(MapToDto(profile));
        return new MemoryStream(bytes);
    }

    public async Task<IReadOnlyList<ResumeProfileAdminSummaryDto>> GetAllProfileSummariesAsync(CancellationToken ct = default)
    {
        var tenantId = _currentUser.TenantId;

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .Select(u => new { u.Id, u.FullName, u.Email, u.Department, u.Designation })
            .ToListAsync(ct);

        var profiles = await _db.ResumeProfiles
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .Select(p => new { p.UserId, p.UpdatedAt })
            .ToListAsync(ct);

        var profileLookup = profiles.ToDictionary(p => p.UserId);

        return users
            .Select(u =>
            {
                var hasProfile = profileLookup.TryGetValue(u.Id, out var rp);
                return new ResumeProfileAdminSummaryDto(
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Department,
                    u.Designation,
                    hasProfile,
                    hasProfile ? rp!.UpdatedAt : null
                );
            })
            .OrderBy(x => x.FullName)
            .ToList()
            .AsReadOnly();
    }

    public async Task<ResumeProfileDto?> GetProfileForAdminAsync(Guid targetUserId, CancellationToken ct = default)
    {
        // Tenant isolation: ensure target user belongs to the caller's tenant
        var profile = await _db.ResumeProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == targetUserId && p.TenantId == _currentUser.TenantId, ct);

        return profile is null ? null : MapToDto(profile);
    }

    public async Task<ResumeProfileDto> SaveProfileForAdminAsync(Guid targetUserId, SaveResumeProfileRequest request, CancellationToken ct = default)
    {
        // Tenant isolation: verify target user exists and belongs to the caller's tenant
        var userExists = await _db.Users
            .AnyAsync(u => u.Id == targetUserId && u.TenantId == _currentUser.TenantId, ct);

        if (!userExists)
            throw new NotFoundException("User", targetUserId);

        var profile = await _db.ResumeProfiles
            .FirstOrDefaultAsync(p => p.UserId == targetUserId && p.TenantId == _currentUser.TenantId, ct);

        if (profile is null)
        {
            profile = new ResumeProfile
            {
                Id = Guid.NewGuid(),
                TenantId = _currentUser.TenantId,
                UserId = targetUserId,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            _db.ResumeProfiles.Add(profile);
        }

        profile.Template = request.Template;
        profile.PersonalInfo = request.PersonalInfo;
        profile.Summary = request.Summary;
        profile.WorkExperience = request.WorkExperience;
        profile.Education = request.Education;
        profile.Skills = request.Skills;
        profile.Certifications = request.Certifications;
        profile.Projects = request.Projects;
        profile.Languages = request.Languages;
        profile.Publications = request.Publications;
        profile.Achievements = request.Achievements;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapToDto(profile);
    }

    private static ResumeProfileDto MapToDto(ResumeProfile p) =>
        new(p.Id, p.UserId, p.Template, p.PersonalInfo, p.Summary,
            p.WorkExperience, p.Education, p.Skills, p.Certifications,
            p.Projects, p.Languages, p.Publications, p.Achievements, p.CreatedAt, p.UpdatedAt);
}
