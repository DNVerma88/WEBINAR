using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KnowHub.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(10);

    public CategoryService(KnowHubDbContext db, ICurrentUserAccessor currentUser, IMemoryCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var cacheKey = $"categories:{_currentUser.TenantId}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<CategoryDto>? cached) && cached is not null)
            return cached;

        var categories = await _db.Categories
            .Where(c => c.TenantId == _currentUser.TenantId && c.IsActive)
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, IconName = c.IconName, SortOrder = c.SortOrder, IsActive = c.IsActive, RecordVersion = c.RecordVersion })
            .ToListAsync(cancellationToken);

        _cache.Set(cacheKey, (IReadOnlyList<CategoryDto>)categories, CacheExpiry);
        return categories;
    }

    public async Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await _db.Categories
            .Where(c => c.Id == id && c.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, IconName = c.IconName, SortOrder = c.SortOrder, IsActive = c.IsActive, RecordVersion = c.RecordVersion })
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null) throw new NotFoundException("Category", id);
        return category;
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admins can manage categories.");

        var exists = await _db.Categories.AnyAsync(c => c.Name == request.Name && c.TenantId == _currentUser.TenantId, cancellationToken);
        if (exists) throw new ConflictException($"A category named '{request.Name}' already exists.");

        var category = new Domain.Entities.Category
        {
            TenantId = _currentUser.TenantId,
            Name = request.Name,
            Description = request.Description,
            IconName = request.IconName,
            SortOrder = request.SortOrder,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCategoryCache();
        return new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description, IconName = category.IconName, SortOrder = category.SortOrder, IsActive = category.IsActive, RecordVersion = category.RecordVersion };
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _currentUser.TenantId, cancellationToken);
        if (category is null) throw new NotFoundException("Category", id);
        if (category.RecordVersion != request.RecordVersion) throw new ConflictException("Category was modified by another session.");

        category.Name = request.Name;
        category.Description = request.Description;
        category.IconName = request.IconName;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        category.ModifiedOn = DateTime.UtcNow;
        category.ModifiedBy = _currentUser.UserId;
        category.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCategoryCache();
        return new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description, IconName = category.IconName, SortOrder = category.SortOrder, IsActive = category.IsActive, RecordVersion = category.RecordVersion };
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _currentUser.TenantId, cancellationToken);
        if (category is null) throw new NotFoundException("Category", id);

        category.IsActive = false;
        category.ModifiedOn = DateTime.UtcNow;
        category.ModifiedBy = _currentUser.UserId;
        category.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCategoryCache();
    }

    private void InvalidateCategoryCache() =>
        _cache.Remove($"categories:{_currentUser.TenantId}");
}
