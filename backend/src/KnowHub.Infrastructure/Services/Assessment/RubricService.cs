using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Assessment;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services.Assessment;

public class RubricService : IRubricService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public RubricService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<RubricDefinitionDto>> GetRubricsAsync(string? designationCode, CancellationToken ct)
    {
        var query = _db.RubricDefinitions
            .Where(r => r.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(designationCode))
            query = query.Where(r => r.DesignationCode == designationCode);

        return await query
            .OrderBy(r => r.DesignationCode)
            .ThenBy(r => r.RatingScaleId)
            .Select(r => new RubricDefinitionDto
            {
                Id                  = r.Id,
                DesignationCode     = r.DesignationCode,
                RatingScaleId       = r.RatingScaleId,
                RatingScaleName     = r.RatingScale.Name,
                RatingScaleValue    = r.RatingScale.NumericValue,
                BehaviorDescription = r.BehaviorDescription,
                ProcessDescription  = r.ProcessDescription,
                EvidenceDescription = r.EvidenceDescription,
                VersionNo           = r.VersionNo,
                EffectiveFrom       = r.EffectiveFrom,
                EffectiveTo         = r.EffectiveTo,
                IsActive            = r.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<List<RubricDefinitionDto>> GetCurrentRubricsForDesignationAsync(string designationCode, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.RubricDefinitions
            .Where(r => r.TenantId == _currentUser.TenantId
                     && r.DesignationCode == designationCode
                     && r.IsActive
                     && r.EffectiveFrom <= today
                     && (r.EffectiveTo == null || r.EffectiveTo >= today))
            .AsNoTracking()
            .OrderBy(r => r.RatingScale.DisplayOrder)
            .Select(r => new RubricDefinitionDto
            {
                Id                  = r.Id,
                DesignationCode     = r.DesignationCode,
                RatingScaleId       = r.RatingScaleId,
                RatingScaleName     = r.RatingScale.Name,
                RatingScaleValue    = r.RatingScale.NumericValue,
                BehaviorDescription = r.BehaviorDescription,
                ProcessDescription  = r.ProcessDescription,
                EvidenceDescription = r.EvidenceDescription,
                VersionNo           = r.VersionNo,
                EffectiveFrom       = r.EffectiveFrom,
                EffectiveTo         = r.EffectiveTo,
                IsActive            = r.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<RubricDefinitionDto> CreateRubricAsync(CreateRubricRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can create rubric definitions.");

        var exists = await _db.RubricDefinitions.AnyAsync(
            r => r.TenantId == _currentUser.TenantId
              && r.DesignationCode == request.DesignationCode
              && r.RatingScaleId == request.RatingScaleId
              && r.IsActive, ct);
        if (exists)
            throw new ConflictException("An active rubric already exists for this designation and rating level.");

        var maxVersion = await _db.RubricDefinitions
            .Where(r => r.TenantId == _currentUser.TenantId && r.DesignationCode == request.DesignationCode)
            .MaxAsync(r => (int?)r.VersionNo, ct) ?? 0;

        var entity = new RubricDefinition
        {
            TenantId            = _currentUser.TenantId,
            DesignationCode     = request.DesignationCode,
            RatingScaleId       = request.RatingScaleId,
            BehaviorDescription = request.BehaviorDescription,
            ProcessDescription  = request.ProcessDescription,
            EvidenceDescription = request.EvidenceDescription,
            VersionNo           = maxVersion + 1,
            EffectiveFrom       = request.EffectiveFrom,
            EffectiveTo         = request.EffectiveTo,
            IsActive            = true,
            CreatedBy           = _currentUser.UserId,
            ModifiedBy          = _currentUser.UserId
        };
        _db.RubricDefinitions.Add(entity);
        await _db.SaveChangesAsync(ct);

        return (await GetRubricsAsync(request.DesignationCode, ct))
            .First(r => r.Id == entity.Id);
    }

    public async Task<RubricDefinitionDto> UpdateRubricAsync(Guid id, UpdateRubricRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can update rubric definitions.");

        var entity = await _db.RubricDefinitions
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("RubricDefinition", id);

        entity.BehaviorDescription = request.BehaviorDescription;
        entity.ProcessDescription  = request.ProcessDescription;
        entity.EvidenceDescription = request.EvidenceDescription;
        entity.EffectiveFrom       = request.EffectiveFrom;
        entity.EffectiveTo         = request.EffectiveTo;
        entity.ModifiedOn          = DateTime.UtcNow;
        entity.ModifiedBy          = _currentUser.UserId;
        entity.RecordVersion++;

        await _db.SaveChangesAsync(ct);

        return (await GetRubricsAsync(entity.DesignationCode, ct))
            .First(r => r.Id == entity.Id);
    }
}
