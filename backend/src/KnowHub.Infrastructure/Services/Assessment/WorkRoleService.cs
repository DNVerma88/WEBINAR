using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Assessment;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services.Assessment;

public class WorkRoleService : IWorkRoleService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public WorkRoleService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<WorkRoleDto>> GetWorkRolesAsync(bool? isActive, CancellationToken ct)
    {
        var query = _db.WorkRoles
            .Where(r => r.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        return await query
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Name)
            .Select(r => new WorkRoleDto
            {
                Id            = r.Id,
                Code          = r.Code,
                Name          = r.Name,
                Category      = r.Category,
                DisplayOrder  = r.DisplayOrder,
                IsActive      = r.IsActive,
                RecordVersion = r.RecordVersion
            })
            .ToListAsync(ct);
    }

    public async Task<WorkRoleDto> CreateWorkRoleAsync(CreateWorkRoleRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can create work roles.");

        var codeExists = await _db.WorkRoles.AnyAsync(
            r => r.Code == request.Code && r.TenantId == _currentUser.TenantId, ct);
        if (codeExists)
            throw new ConflictException($"A work role with code '{request.Code}' already exists.");

        var entity = new WorkRole
        {
            TenantId     = _currentUser.TenantId,
            Code         = request.Code,
            Name         = request.Name,
            Category     = request.Category,
            DisplayOrder = request.DisplayOrder,
            IsActive     = true,
            CreatedBy    = _currentUser.UserId,
            ModifiedBy   = _currentUser.UserId
        };
        _db.WorkRoles.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new WorkRoleDto
        {
            Id            = entity.Id,
            Code          = entity.Code,
            Name          = entity.Name,
            Category      = entity.Category,
            DisplayOrder  = entity.DisplayOrder,
            IsActive      = entity.IsActive,
            RecordVersion = entity.RecordVersion
        };
    }

    public async Task<WorkRoleDto> UpdateWorkRoleAsync(Guid id, UpdateWorkRoleRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can update work roles.");

        var entity = await _db.WorkRoles
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("WorkRole", id);

        if (entity.RecordVersion != request.RecordVersion)
            throw new ConflictException("The work role has been modified by another user. Please refresh and try again.");

        entity.Name          = request.Name;
        entity.Category      = request.Category;
        entity.DisplayOrder  = request.DisplayOrder;
        entity.IsActive      = request.IsActive;
        entity.ModifiedOn    = DateTime.UtcNow;
        entity.ModifiedBy    = _currentUser.UserId;
        entity.RecordVersion++;

        await _db.SaveChangesAsync(ct);

        return new WorkRoleDto
        {
            Id            = entity.Id,
            Code          = entity.Code,
            Name          = entity.Name,
            Category      = entity.Category,
            DisplayOrder  = entity.DisplayOrder,
            IsActive      = entity.IsActive,
            RecordVersion = entity.RecordVersion
        };
    }
}
