using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class KnowledgeBundleService : IKnowledgeBundleService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public KnowledgeBundleService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<KnowledgeBundleDto>> GetBundlesAsync(
        GetBundlesRequest request, CancellationToken cancellationToken)
    {
        var query = _db.KnowledgeBundles
            .Where(b => b.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (!_currentUser.IsAdminOrAbove) query = query.Where(b => b.IsPublished);
        if (request.CategoryId.HasValue) query = query.Where(b => b.CategoryId == request.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(b => b.Title.Contains(request.SearchTerm));

        var (data, total) = await query
            .OrderByDescending(b => b.CreatedDate)
            .Select(b => new KnowledgeBundleDto
            {
                Id = b.Id, Title = b.Title, Description = b.Description,
                CategoryId = b.CategoryId,
                CategoryName = b.Category != null ? b.Category.Name : null,
                IsPublished = b.IsPublished, CoverImageUrl = b.CoverImageUrl,
                CreatedByUserId = b.CreatedByUserId,
                CreatedByUserName = b.CreatedByUser != null ? b.CreatedByUser.FullName : string.Empty,
                ItemCount = b.Items.Count, CreatedDate = b.CreatedDate
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResult<KnowledgeBundleDto>
        {
            Data = data, TotalCount = total,
            PageNumber = request.PageNumber, PageSize = request.PageSize
        };
    }

    public async Task<KnowledgeBundleDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var bundle = await _db.KnowledgeBundles
            .Include(b => b.Category)
            .Include(b => b.CreatedByUser)
            .Include(b => b.Items.OrderBy(i => i.OrderSequence))
                .ThenInclude(i => i.KnowledgeAsset)
            .Where(b => b.Id == id && b.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (bundle is null) throw new NotFoundException("KnowledgeBundle", id);
        if (!bundle.IsPublished && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("This bundle is not published.");

        return new KnowledgeBundleDetailDto
        {
            Id = bundle.Id, Title = bundle.Title, Description = bundle.Description,
            CategoryId = bundle.CategoryId, CategoryName = bundle.Category?.Name,
            IsPublished = bundle.IsPublished, CoverImageUrl = bundle.CoverImageUrl,
            CreatedByUserId = bundle.CreatedByUserId,
            CreatedByUserName = bundle.CreatedByUser?.FullName ?? string.Empty,
            ItemCount = bundle.Items.Count, CreatedDate = bundle.CreatedDate,
            Items = bundle.Items.Select(i => new KnowledgeBundleItemDto
            {
                Id = i.Id,
                KnowledgeAssetId = i.KnowledgeAssetId,
                AssetTitle = i.KnowledgeAsset?.Title ?? string.Empty,
                AssetUrl = i.KnowledgeAsset?.Url ?? string.Empty,
                AssetType = i.KnowledgeAsset?.AssetType.ToString() ?? string.Empty,
                OrderSequence = i.OrderSequence, Notes = i.Notes
            }).ToList()
        };
    }

    public async Task<KnowledgeBundleDto> CreateAsync(
        CreateKnowledgeBundleRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove && !_currentUser.IsInRole(UserRole.Contributor))
            throw new ForbiddenException("Only Contributors or Admins may create knowledge bundles.");

        var bundle = new KnowledgeBundle
        {
            TenantId = _currentUser.TenantId,
            Title = request.Title,
            Description = request.Description,
            CategoryId = request.CategoryId,
            CoverImageUrl = request.CoverImageUrl,
            IsPublished = false,
            CreatedByUserId = _currentUser.UserId,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.KnowledgeBundles.Add(bundle);
        await _db.SaveChangesAsync(cancellationToken);

        return new KnowledgeBundleDto
        {
            Id = bundle.Id, Title = bundle.Title, Description = bundle.Description,
            CategoryId = bundle.CategoryId, IsPublished = bundle.IsPublished,
            CoverImageUrl = bundle.CoverImageUrl,
            CreatedByUserId = bundle.CreatedByUserId, ItemCount = 0,
            CreatedDate = bundle.CreatedDate
        };
    }

    public async Task<KnowledgeBundleDto> UpdateAsync(
        Guid id, UpdateKnowledgeBundleRequest request, CancellationToken cancellationToken)
    {
        var bundle = await _db.KnowledgeBundles
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == _currentUser.TenantId, cancellationToken);

        if (bundle is null) throw new NotFoundException("KnowledgeBundle", id);
        if (!_currentUser.IsAdminOrAbove && bundle.CreatedByUserId != _currentUser.UserId)
            throw new ForbiddenException("Only the bundle owner or an Admin may update this bundle.");

        bundle.Title = request.Title;
        bundle.Description = request.Description;
        bundle.CategoryId = request.CategoryId;
        bundle.CoverImageUrl = request.CoverImageUrl;
        bundle.IsPublished = request.IsPublished;
        bundle.ModifiedBy = _currentUser.UserId;
        bundle.ModifiedOn = DateTime.UtcNow;
        bundle.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);

        return new KnowledgeBundleDto
        {
            Id = bundle.Id, Title = bundle.Title, Description = bundle.Description,
            CategoryId = bundle.CategoryId, IsPublished = bundle.IsPublished,
            CoverImageUrl = bundle.CoverImageUrl,
            CreatedByUserId = bundle.CreatedByUserId,
            ItemCount = bundle.Items.Count, CreatedDate = bundle.CreatedDate
        };
    }

    public async Task AddItemAsync(Guid bundleId, AddBundleItemRequest request, CancellationToken cancellationToken)
    {
        var bundle = await _db.KnowledgeBundles
            .FirstOrDefaultAsync(b => b.Id == bundleId && b.TenantId == _currentUser.TenantId, cancellationToken);
        if (bundle is null) throw new NotFoundException("KnowledgeBundle", bundleId);

        if (!_currentUser.IsAdminOrAbove && bundle.CreatedByUserId != _currentUser.UserId)
            throw new ForbiddenException("Only the bundle owner or an Admin may add items.");

        var assetExists = await _db.KnowledgeAssets.AnyAsync(
            a => a.Id == request.KnowledgeAssetId && a.TenantId == _currentUser.TenantId, cancellationToken);
        if (!assetExists) throw new NotFoundException("KnowledgeAsset", request.KnowledgeAssetId);

        var alreadyAdded = await _db.KnowledgeBundleItems.AnyAsync(
            i => i.BundleId == bundleId
                && i.KnowledgeAssetId == request.KnowledgeAssetId
                && i.TenantId == _currentUser.TenantId, cancellationToken);
        if (alreadyAdded) throw new ConflictException("Asset already exists in this bundle.");

        var item = new KnowledgeBundleItem
        {
            TenantId = _currentUser.TenantId,
            BundleId = bundleId,
            KnowledgeAssetId = request.KnowledgeAssetId,
            OrderSequence = request.OrderSequence,
            Notes = request.Notes,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.KnowledgeBundleItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveItemAsync(Guid bundleId, Guid assetId, CancellationToken cancellationToken)
    {
        var item = await _db.KnowledgeBundleItems
            .FirstOrDefaultAsync(i => i.BundleId == bundleId
                && i.KnowledgeAssetId == assetId
                && i.TenantId == _currentUser.TenantId, cancellationToken);

        if (item is null) throw new NotFoundException("Bundle item not found.");

        var bundle = await _db.KnowledgeBundles
            .FirstOrDefaultAsync(b => b.Id == bundleId, cancellationToken);

        if (bundle is not null && !_currentUser.IsAdminOrAbove && bundle.CreatedByUserId != _currentUser.UserId)
            throw new ForbiddenException("Only the bundle owner or an Admin may remove items.");

        _db.KnowledgeBundleItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
