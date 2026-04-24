using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Assessment;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services.Assessment;

public class ParameterService : IParameterService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public ParameterService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<ParameterMasterDto>> GetParametersAsync(CancellationToken ct)
    {
        return await _db.ParameterMasters
            .Where(p => p.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new ParameterMasterDto
            {
                Id           = p.Id,
                Name         = p.Name,
                Code         = p.Code,
                Description  = p.Description,
                Category     = p.Category,
                DisplayOrder = p.DisplayOrder,
                IsActive     = p.IsActive,
                RecordVersion = p.RecordVersion
            })
            .ToListAsync(ct);
    }

    public async Task<ParameterMasterDto> CreateParameterAsync(CreateParameterRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can create parameters.");

        var exists = await _db.ParameterMasters.AnyAsync(
            p => p.Code == request.Code && p.TenantId == _currentUser.TenantId, ct);
        if (exists)
            throw new ConflictException($"A parameter with code '{request.Code}' already exists.");

        var entity = new ParameterMaster
        {
            TenantId     = _currentUser.TenantId,
            Name         = request.Name,
            Code         = request.Code,
            Description  = request.Description,
            Category     = request.Category,
            DisplayOrder = request.DisplayOrder,
            IsActive     = true,
            CreatedBy    = _currentUser.UserId,
            ModifiedBy   = _currentUser.UserId
        };
        _db.ParameterMasters.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new ParameterMasterDto
        {
            Id = entity.Id, Name = entity.Name, Code = entity.Code, Description = entity.Description,
            Category = entity.Category, DisplayOrder = entity.DisplayOrder,
            IsActive = entity.IsActive, RecordVersion = entity.RecordVersion
        };
    }

    public async Task<ParameterMasterDto> UpdateParameterAsync(Guid id, UpdateParameterRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can update parameters.");

        var entity = await _db.ParameterMasters
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("ParameterMaster", id);

        if (entity.RecordVersion != request.RecordVersion)
            throw new ConflictException("The parameter has been modified by another user. Please refresh.");

        entity.Name         = request.Name;
        entity.Description  = request.Description;
        entity.Category     = request.Category;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive     = request.IsActive;
        entity.ModifiedOn   = DateTime.UtcNow;
        entity.ModifiedBy   = _currentUser.UserId;
        entity.RecordVersion++;

        await _db.SaveChangesAsync(ct);

        return new ParameterMasterDto
        {
            Id = entity.Id, Name = entity.Name, Code = entity.Code, Description = entity.Description,
            Category = entity.Category, DisplayOrder = entity.DisplayOrder,
            IsActive = entity.IsActive, RecordVersion = entity.RecordVersion
        };
    }

    public async Task<List<RoleParameterMappingDto>> GetRoleMappingsAsync(string? designationCode, CancellationToken ct)
    {
        var query = _db.RoleParameterMappings
            .Where(m => m.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(designationCode))
            query = query.Where(m => m.DesignationCode == designationCode);

        return await query
            .OrderBy(m => m.DesignationCode)
            .ThenBy(m => m.DisplayOrder)
            .Select(m => new RoleParameterMappingDto
            {
                Id              = m.Id,
                DesignationCode = m.DesignationCode,
                ParameterId     = m.ParameterId,
                ParameterName   = m.Parameter.Name,
                Weightage       = m.Weightage,
                DisplayOrder    = m.DisplayOrder,
                IsMandatory     = m.IsMandatory,
                IsActive        = m.IsActive,
                RecordVersion   = m.RecordVersion
            })
            .ToListAsync(ct);
    }

    public async Task<RoleParameterMappingDto> UpsertRoleMappingAsync(UpsertRoleMappingRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can configure role-parameter mappings.");

        var existing = await _db.RoleParameterMappings
            .FirstOrDefaultAsync(m => m.TenantId == _currentUser.TenantId
                                   && m.DesignationCode == request.DesignationCode
                                   && m.ParameterId == request.ParameterId, ct);

        if (existing is not null)
        {
            existing.Weightage    = request.Weightage;
            existing.DisplayOrder = request.DisplayOrder;
            existing.IsMandatory  = request.IsMandatory;
            existing.IsActive     = true;
            existing.ModifiedOn   = DateTime.UtcNow;
            existing.ModifiedBy   = _currentUser.UserId;
            existing.RecordVersion++;
        }
        else
        {
            existing = new RoleParameterMapping
            {
                TenantId        = _currentUser.TenantId,
                DesignationCode = request.DesignationCode,
                ParameterId     = request.ParameterId,
                Weightage       = request.Weightage,
                DisplayOrder    = request.DisplayOrder,
                IsMandatory     = request.IsMandatory,
                IsActive        = true,
                CreatedBy       = _currentUser.UserId,
                ModifiedBy      = _currentUser.UserId
            };
            _db.RoleParameterMappings.Add(existing);
        }

        await _db.SaveChangesAsync(ct);

        return (await GetRoleMappingsAsync(request.DesignationCode, ct))
            .First(m => m.Id == existing.Id);
    }

    public async Task RemoveRoleMappingAsync(Guid id, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can remove role-parameter mappings.");

        var entity = await _db.RoleParameterMappings
            .FirstOrDefaultAsync(m => m.Id == id && m.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("RoleParameterMapping", id);

        entity.IsActive   = false;
        entity.ModifiedOn = DateTime.UtcNow;
        entity.ModifiedBy = _currentUser.UserId;
        entity.RecordVersion++;

        await _db.SaveChangesAsync(ct);
    }
}
