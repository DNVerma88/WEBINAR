using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class LearningPathCohortService : ILearningPathCohortService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public LearningPathCohortService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<LearningPathCohortDto>> GetCohortsAsync(
        Guid learningPathId, CancellationToken cancellationToken)
    {
        await EnsureLearningPathExistsAsync(learningPathId, cancellationToken);

        return await _db.LearningPathCohorts
            .Where(c => c.LearningPathId == learningPathId && c.TenantId == _currentUser.TenantId)
            .OrderBy(c => c.StartDate)
            .Select(c => ToDto(c))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<LearningPathCohortDto> CreateCohortAsync(
        Guid learningPathId, CreateLearningPathCohortRequest request, CancellationToken cancellationToken)
    {
        RequireAdminOrKt();
        await EnsureLearningPathExistsAsync(learningPathId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessRuleException("Cohort name is required.");
        if (request.EndDate.HasValue && request.EndDate.Value <= request.StartDate)
            throw new BusinessRuleException("End date must be after start date.");

        var cohort = new LearningPathCohort
        {
            TenantId = _currentUser.TenantId,
            LearningPathId = learningPathId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MaxParticipants = request.MaxParticipants,
            Status = CohortStatus.Scheduled,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId,
        };

        _db.LearningPathCohorts.Add(cohort);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(cohort);
    }

    public async Task<LearningPathCohortDto> UpdateCohortAsync(
        Guid learningPathId, Guid cohortId, UpdateLearningPathCohortRequest request, CancellationToken cancellationToken)
    {
        RequireAdminOrKt();

        var cohort = await _db.LearningPathCohorts
            .FirstOrDefaultAsync(c => c.Id == cohortId
                && c.LearningPathId == learningPathId
                && c.TenantId == _currentUser.TenantId, cancellationToken);

        if (cohort is null) throw new NotFoundException("LearningPathCohort", cohortId);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessRuleException("Cohort name is required.");
        if (request.EndDate.HasValue && request.EndDate.Value <= request.StartDate)
            throw new BusinessRuleException("End date must be after start date.");

        cohort.Name = request.Name.Trim();
        cohort.Description = request.Description?.Trim();
        cohort.StartDate = request.StartDate;
        cohort.EndDate = request.EndDate;
        cohort.MaxParticipants = request.MaxParticipants;
        cohort.Status = request.Status;
        cohort.ModifiedBy = _currentUser.UserId;
        cohort.ModifiedOn = DateTime.UtcNow;
        cohort.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(cohort);
    }

    public async Task DeleteCohortAsync(
        Guid learningPathId, Guid cohortId, CancellationToken cancellationToken)
    {
        RequireAdminOrKt();

        var cohort = await _db.LearningPathCohorts
            .FirstOrDefaultAsync(c => c.Id == cohortId
                && c.LearningPathId == learningPathId
                && c.TenantId == _currentUser.TenantId, cancellationToken);

        if (cohort is null) throw new NotFoundException("LearningPathCohort", cohortId);

        _db.LearningPathCohorts.Remove(cohort);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private void RequireAdminOrKt()
    {
        if (!_currentUser.IsAdminOrAbove && !_currentUser.IsInRole(UserRole.KnowledgeTeam))
            throw new ForbiddenException("Only Admin or KnowledgeTeam may manage cohorts.");
    }

    private async Task EnsureLearningPathExistsAsync(Guid learningPathId, CancellationToken cancellationToken)
    {
        var exists = await _db.LearningPaths
            .AnyAsync(p => p.Id == learningPathId && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (!exists) throw new NotFoundException("LearningPath", learningPathId);
    }

    private static LearningPathCohortDto ToDto(LearningPathCohort c) => new()
    {
        Id = c.Id,
        LearningPathId = c.LearningPathId,
        Name = c.Name,
        Description = c.Description,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        MaxParticipants = c.MaxParticipants,
        Status = c.Status,
        CreatedDate = c.CreatedDate,
    };
}
