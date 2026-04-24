using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Assessment;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services.Assessment;

public class RatingScaleService : IRatingScaleService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public RatingScaleService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<RatingScaleDto>> GetScalesAsync(CancellationToken ct)
    {
        return await _db.RatingScales
            .Where(s => s.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new RatingScaleDto
            {
                Id           = s.Id,
                Code         = s.Code,
                Name         = s.Name,
                NumericValue = s.NumericValue,
                DisplayOrder = s.DisplayOrder,
                IsActive     = s.IsActive,
                RecordVersion = s.RecordVersion
            })
            .ToListAsync(ct);
    }

    public async Task<RatingScaleDto> CreateScaleAsync(CreateRatingScaleRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can create rating scales.");

        var exists = await _db.RatingScales.AnyAsync(
            s => s.Code == request.Code && s.TenantId == _currentUser.TenantId, ct);
        if (exists)
            throw new ConflictException($"A rating scale with code '{request.Code}' already exists.");

        var entity = new RatingScale
        {
            TenantId     = _currentUser.TenantId,
            Code         = request.Code,
            Name         = request.Name,
            NumericValue = request.NumericValue,
            DisplayOrder = request.DisplayOrder,
            IsActive     = true,
            CreatedBy    = _currentUser.UserId,
            ModifiedBy   = _currentUser.UserId
        };
        _db.RatingScales.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new RatingScaleDto
        {
            Id = entity.Id, Code = entity.Code, Name = entity.Name,
            NumericValue = entity.NumericValue, DisplayOrder = entity.DisplayOrder,
            IsActive = entity.IsActive, RecordVersion = entity.RecordVersion
        };
    }

    public async Task<RatingScaleDto> UpdateScaleAsync(Guid id, UpdateRatingScaleRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can update rating scales.");

        var entity = await _db.RatingScales
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("RatingScale", id);

        if (entity.RecordVersion != request.RecordVersion)
            throw new ConflictException("The rating scale has been modified by another user. Please refresh.");

        entity.Code         = request.Code;
        entity.Name         = request.Name;
        entity.NumericValue = request.NumericValue;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive     = request.IsActive;
        entity.ModifiedOn   = DateTime.UtcNow;
        entity.ModifiedBy   = _currentUser.UserId;
        entity.RecordVersion++;

        await _db.SaveChangesAsync(ct);

        return new RatingScaleDto
        {
            Id = entity.Id, Code = entity.Code, Name = entity.Name,
            NumericValue = entity.NumericValue, DisplayOrder = entity.DisplayOrder,
            IsActive = entity.IsActive, RecordVersion = entity.RecordVersion
        };
    }

    public async Task DeactivateScaleAsync(Guid id, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can deactivate rating scales.");

        var entity = await _db.RatingScales
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("RatingScale", id);

        entity.IsActive   = false;
        entity.ModifiedOn = DateTime.UtcNow;
        entity.ModifiedBy = _currentUser.UserId;
        entity.RecordVersion++;

        await _db.SaveChangesAsync(ct);
    }
}
