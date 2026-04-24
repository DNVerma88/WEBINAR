using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class KnowledgeAssetService : IKnowledgeAssetService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IXpService _xpService;

    public KnowledgeAssetService(KnowHubDbContext db, ICurrentUserAccessor currentUser, IXpService xpService)
    {
        _db = db;
        _currentUser = currentUser;
        _xpService = xpService;
    }

    public async Task<PagedResult<KnowledgeAssetDto>> GetAssetsAsync(
        GetAssetsRequest request, CancellationToken cancellationToken)
    {
        var query = _db.KnowledgeAssets
            .Where(a => a.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (!_currentUser.IsAdminOrAbove) query = query.Where(a => a.IsPublic);
        if (request.SessionId.HasValue) query = query.Where(a => a.SessionId == request.SessionId.Value);
        if (request.AssetType.HasValue) query = query.Where(a => a.AssetType == request.AssetType.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(a => a.Title.Contains(request.SearchTerm));

        var (data, total) = await query
            .OrderByDescending(a => a.CreatedDate)
            .Select(a => new KnowledgeAssetDto
            {
                Id = a.Id, SessionId = a.SessionId, Title = a.Title,
                Url = a.Url, Description = a.Description,
                AssetType = a.AssetType, ViewCount = a.ViewCount,
                DownloadCount = a.DownloadCount, IsPublic = a.IsPublic,
                IsVerified = a.IsVerified,
                CreatedDate = a.CreatedDate, CreatedBy = a.CreatedBy
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResult<KnowledgeAssetDto>
        {
            Data = data, TotalCount = total,
            PageNumber = request.PageNumber, PageSize = request.PageSize
        };
    }

    public async Task<KnowledgeAssetDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var asset = await _db.KnowledgeAssets
            .Where(a => a.Id == id && a.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (asset is null) throw new NotFoundException("KnowledgeAsset", id);
        if (!asset.IsPublic && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("This asset is not publicly accessible.");

        return new KnowledgeAssetDto
        {
            Id = asset.Id, SessionId = asset.SessionId, Title = asset.Title,
            Url = asset.Url, Description = asset.Description,
            AssetType = asset.AssetType, ViewCount = asset.ViewCount,
            DownloadCount = asset.DownloadCount, IsPublic = asset.IsPublic,
            IsVerified = asset.IsVerified,
            CreatedDate = asset.CreatedDate, CreatedBy = asset.CreatedBy
        };
    }

    public async Task<KnowledgeAssetDto> CreateAsync(
        CreateKnowledgeAssetRequest request, CancellationToken cancellationToken)
    {
        if (request.SessionId.HasValue)
        {
            var sessionExists = await _db.Sessions.AnyAsync(
                s => s.Id == request.SessionId.Value && s.TenantId == _currentUser.TenantId, cancellationToken);
            if (!sessionExists) throw new NotFoundException("Session", request.SessionId.Value);
        }

        var asset = new KnowledgeAsset
        {
            TenantId = _currentUser.TenantId,
            SessionId = request.SessionId,
            Title = request.Title,
            Url = request.Url,
            Description = request.Description,
            AssetType = request.AssetType,
            IsPublic = request.IsPublic,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.KnowledgeAssets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);

        await _xpService.AwardXpAsync(new AwardXpRequest
        {
            UserId = _currentUser.UserId,
            TenantId = _currentUser.TenantId,
            EventType = XpEventType.UploadAsset,
            RelatedEntityType = "KnowledgeAsset",
            RelatedEntityId = asset.Id
        }, cancellationToken);

        return new KnowledgeAssetDto
        {
            Id = asset.Id, SessionId = asset.SessionId, Title = asset.Title,
            Url = asset.Url, Description = asset.Description,
            AssetType = asset.AssetType, ViewCount = asset.ViewCount,
            DownloadCount = asset.DownloadCount, IsPublic = asset.IsPublic,
            IsVerified = asset.IsVerified,
            CreatedDate = asset.CreatedDate, CreatedBy = asset.CreatedBy
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var asset = await _db.KnowledgeAssets
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _currentUser.TenantId, cancellationToken);

        if (asset is null) throw new NotFoundException("KnowledgeAsset", id);
        if (!_currentUser.IsAdminOrAbove && asset.CreatedBy != _currentUser.UserId)
            throw new ForbiddenException("Only the asset owner or an Admin may delete this asset.");

        _db.KnowledgeAssets.Remove(asset);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
