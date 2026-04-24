using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class LearningPathService : ILearningPathService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IXpService _xpService;

    public LearningPathService(KnowHubDbContext db, ICurrentUserAccessor currentUser, IXpService xpService)
    {
        _db = db;
        _currentUser = currentUser;
        _xpService = xpService;
    }

    public async Task<PagedResult<LearningPathDto>> GetPathsAsync(
        GetLearningPathsRequest request, CancellationToken cancellationToken)
    {
        var query = _db.LearningPaths
            .Where(p => p.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (!_currentUser.IsAdminOrAbove) query = query.Where(p => p.IsPublished);
        if (request.IsPublished.HasValue) query = query.Where(p => p.IsPublished == request.IsPublished.Value);
        if (request.CategoryId.HasValue) query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        if (request.DifficultyLevel.HasValue) query = query.Where(p => p.DifficultyLevel == request.DifficultyLevel.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(p => p.Title.Contains(request.SearchTerm));

        var (data, total) = await query
            .OrderBy(p => p.Title)
            .Select(p => new LearningPathDto
            {
                Id = p.Id, Title = p.Title, Slug = p.Slug,
                Description = p.Description, Objective = p.Objective,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                DifficultyLevel = p.DifficultyLevel,
                EstimatedDurationMinutes = p.EstimatedDurationMinutes,
                IsPublished = p.IsPublished, IsAssignable = p.IsAssignable,
                CoverImageUrl = p.CoverImageUrl,
                ItemCount = p.Items.Count
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResult<LearningPathDto>
        {
            Data = data, TotalCount = total,
            PageNumber = request.PageNumber, PageSize = request.PageSize
        };
    }

    public async Task<LearningPathDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var path = await _db.LearningPaths
            .Include(p => p.Category)
            .Include(p => p.Items)
                .ThenInclude(i => i.Session)
            .Include(p => p.Items)
                .ThenInclude(i => i.KnowledgeAsset)
            .Where(p => p.Id == id && p.TenantId == _currentUser.TenantId)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (path is null) throw new NotFoundException("LearningPath", id);
        if (!path.IsPublished && !_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Access to unpublished learning path is restricted.");

        var enrolledCount = await _db.UserLearningPathEnrollments
            .CountAsync(e => e.LearningPathId == id && e.TenantId == _currentUser.TenantId, cancellationToken);

        return new LearningPathDetailDto
        {
            Id = path.Id, Title = path.Title, Slug = path.Slug,
            Description = path.Description, Objective = path.Objective,
            CategoryId = path.CategoryId, CategoryName = path.Category?.Name,
            DifficultyLevel = path.DifficultyLevel,
            EstimatedDurationMinutes = path.EstimatedDurationMinutes,
            IsPublished = path.IsPublished, IsAssignable = path.IsAssignable,
            CoverImageUrl = path.CoverImageUrl, ItemCount = path.Items.Count,
            EnrolledCount = enrolledCount,
            Items = path.Items.OrderBy(i => i.OrderSequence).Select(i => new LearningPathItemDto
            {
                Id = i.Id, ItemType = i.ItemType,
                SessionId = i.SessionId, SessionTitle = i.Session?.Title,
                KnowledgeAssetId = i.KnowledgeAssetId, AssetTitle = i.KnowledgeAsset?.Title,
                OrderSequence = i.OrderSequence, IsRequired = i.IsRequired
            }).ToList()
        };
    }

    public async Task<LearningPathDto> CreateAsync(
        CreateLearningPathRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove && !_currentUser.IsInRole(UserRole.KnowledgeTeam))
            throw new ForbiddenException("Only Admin or KnowledgeTeam may create learning paths.");

        var slug = GenerateSlug(request.Title);
        var slugExists = await _db.LearningPaths
            .AnyAsync(p => p.TenantId == _currentUser.TenantId && p.Slug == slug, cancellationToken);
        if (slugExists) slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";

        var path = new LearningPath
        {
            TenantId = _currentUser.TenantId,
            Title = request.Title, Slug = slug,
            Description = request.Description, Objective = request.Objective,
            CategoryId = request.CategoryId,
            DifficultyLevel = request.DifficultyLevel,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            CoverImageUrl = request.CoverImageUrl,
            IsPublished = false, IsAssignable = true,
            CreatedBy = _currentUser.UserId, ModifiedBy = _currentUser.UserId
        };

        _db.LearningPaths.Add(path);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(path, 0);
    }

    public async Task<LearningPathDto> UpdateAsync(
        Guid id, UpdateLearningPathRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove && !_currentUser.IsInRole(UserRole.KnowledgeTeam))
            throw new ForbiddenException("Only Admin or KnowledgeTeam may update learning paths.");

        var path = await _db.LearningPaths
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (path is null) throw new NotFoundException("LearningPath", id);

        path.Title = request.Title;
        path.Description = request.Description;
        path.Objective = request.Objective;
        path.CategoryId = request.CategoryId;
        path.DifficultyLevel = request.DifficultyLevel;
        path.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        path.CoverImageUrl = request.CoverImageUrl;
        path.IsPublished = request.IsPublished;
        path.ModifiedBy = _currentUser.UserId;
        path.ModifiedOn = DateTime.UtcNow;
        path.RecordVersion++;

        await _db.SaveChangesAsync(cancellationToken);

        var itemCount = await _db.LearningPathItems.CountAsync(i => i.LearningPathId == id, cancellationToken);
        return ToDto(path, itemCount);
    }

    public async Task EnrolAsync(Guid pathId, CancellationToken cancellationToken)
    {
        var path = await _db.LearningPaths
            .FirstOrDefaultAsync(p => p.Id == pathId && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (path is null) throw new NotFoundException("LearningPath", pathId);
        if (!path.IsPublished) throw new BusinessRuleException("Cannot enrol in an unpublished learning path.");

        var existing = await _db.UserLearningPathEnrollments
            .AnyAsync(e => e.TenantId == _currentUser.TenantId
                && e.UserId == _currentUser.UserId
                && e.LearningPathId == pathId, cancellationToken);
        if (existing) throw new ConflictException("Already enrolled in this learning path.");

        var enrolment = new UserLearningPathEnrollment
        {
            TenantId = _currentUser.TenantId,
            UserId = _currentUser.UserId,
            LearningPathId = pathId,
            EnrolmentType = EnrolmentType.SelfEnrolled,
            ProgressPercentage = 0,
            CompletedItemCount = 0,
            StartedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.UserLearningPathEnrollments.Add(enrolment);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnenrolAsync(Guid pathId, CancellationToken cancellationToken)
    {
        var enrolment = await _db.UserLearningPathEnrollments
            .FirstOrDefaultAsync(e => e.TenantId == _currentUser.TenantId
                && e.UserId == _currentUser.UserId
                && e.LearningPathId == pathId, cancellationToken);

        if (enrolment is null) throw new NotFoundException("Enrolment not found.");

        _db.UserLearningPathEnrollments.Remove(enrolment);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<EnrolmentProgressDto> GetProgressAsync(Guid pathId, CancellationToken cancellationToken)
    {
        var enrolment = await _db.UserLearningPathEnrollments
            .FirstOrDefaultAsync(e => e.TenantId == _currentUser.TenantId
                && e.UserId == _currentUser.UserId
                && e.LearningPathId == pathId, cancellationToken);

        if (enrolment is null) throw new NotFoundException("Enrolment not found.");

        var totalItemCount = await _db.LearningPathItems
            .CountAsync(i => i.LearningPathId == pathId, cancellationToken);

        return new EnrolmentProgressDto
        {
            UserId = _currentUser.UserId,
            LearningPathId = pathId,
            ProgressPercentage = enrolment.ProgressPercentage,
            CompletedItemCount = enrolment.CompletedItemCount,
            TotalItemCount = totalItemCount,
            StartedAt = enrolment.StartedAt,
            CompletedAt = enrolment.CompletedAt
        };
    }

    public async Task<LearningPathCertificateDto> GetCertificateAsync(Guid pathId, CancellationToken cancellationToken)
    {
        var enrolment = await _db.UserLearningPathEnrollments
            .FirstOrDefaultAsync(e => e.TenantId == _currentUser.TenantId
                && e.UserId == _currentUser.UserId
                && e.LearningPathId == pathId, cancellationToken);

        if (enrolment is null) throw new NotFoundException("Enrolment not found.");
        if (enrolment.ProgressPercentage < 100)
            throw new BusinessRuleException("Certificate is only available upon 100% completion.");

        var cert = await _db.LearningPathCertificates
            .Include(c => c.LearningPath)
            .FirstOrDefaultAsync(c => c.TenantId == _currentUser.TenantId
                && c.UserId == _currentUser.UserId
                && c.LearningPathId == pathId, cancellationToken);

        if (cert is null)
            cert = await IssueCertificateAsync(enrolment, cancellationToken);

        return new LearningPathCertificateDto
        {
            Id = cert.Id,
            CertificateNumber = cert.CertificateNumber,
            CertificateUrl = cert.CertificateUrl,
            IssuedAt = cert.IssuedAt,
            PathTitle = cert.LearningPath?.Title ?? string.Empty
        };
    }

    public async Task<List<UserEnrolmentDto>> GetUserEnrolmentsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove && _currentUser.UserId != userId)
            throw new ForbiddenException("Cannot view another user's enrolments.");

        return await _db.UserLearningPathEnrollments
            .Include(e => e.LearningPath)
            .Where(e => e.TenantId == _currentUser.TenantId && e.UserId == userId)
            .OrderByDescending(e => e.StartedAt)
            .Select(e => new UserEnrolmentDto
            {
                EnrolmentId = e.Id,
                LearningPathId = e.LearningPathId,
                PathTitle = e.LearningPath != null ? e.LearningPath.Title : string.Empty,
                ProgressPercentage = e.ProgressPercentage,
                EnrolmentType = e.EnrolmentType,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                DeadlineAt = e.DeadlineAt
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private async Task<LearningPathCertificate> IssueCertificateAsync(
        UserLearningPathEnrollment enrolment, CancellationToken cancellationToken)
    {
        var certNumber = GenerateCertificateNumber();
        var cert = new LearningPathCertificate
        {
            TenantId = enrolment.TenantId,
            UserId = enrolment.UserId,
            LearningPathId = enrolment.LearningPathId,
            CertificateNumber = certNumber,
            CertificateUrl = $"/certificates/{certNumber}",
            IssuedAt = DateTime.UtcNow,
            CreatedBy = enrolment.UserId,
            ModifiedBy = enrolment.UserId
        };

        _db.LearningPathCertificates.Add(cert);
        await _db.SaveChangesAsync(cancellationToken);

        await _xpService.AwardXpAsync(new AwardXpRequest
        {
            UserId = enrolment.UserId,
            TenantId = enrolment.TenantId,
            EventType = XpEventType.LearningPathCompleted,
            RelatedEntityType = "LearningPath",
            RelatedEntityId = enrolment.LearningPathId
        }, cancellationToken);

        return await _db.LearningPathCertificates
            .Include(c => c.LearningPath)
            .FirstAsync(c => c.Id == cert.Id, cancellationToken);
    }

    public async Task<LearningPathItemDto> AddItemAsync(
        Guid pathId, AddLearningPathItemRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove && !_currentUser.IsInRole(UserRole.KnowledgeTeam))
            throw new ForbiddenException("Only Admin or KnowledgeTeam may manage learning path content.");

        var path = await _db.LearningPaths
            .FirstOrDefaultAsync(p => p.Id == pathId && p.TenantId == _currentUser.TenantId, cancellationToken);
        if (path is null) throw new NotFoundException("LearningPath", pathId);

        var nextOrder = await _db.LearningPathItems
            .Where(i => i.LearningPathId == pathId)
            .MaxAsync(i => (int?)i.OrderSequence, cancellationToken) ?? 0;

        var item = new LearningPathItem
        {
            LearningPathId = pathId,
            ItemType = request.ItemType,
            SessionId = request.ItemType == LearningPathItemType.Session ? request.SessionId : null,
            KnowledgeAssetId = request.ItemType == LearningPathItemType.KnowledgeAsset ? request.KnowledgeAssetId : null,
            OrderSequence = nextOrder + 1,
            IsRequired = request.IsRequired,
            TenantId = _currentUser.TenantId,
            CreatedBy = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };

        _db.LearningPathItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        string? sessionTitle = null;
        string? assetTitle = null;
        if (item.SessionId.HasValue)
            sessionTitle = await _db.Sessions
                .Where(s => s.Id == item.SessionId.Value)
                .Select(s => s.Title)
                .FirstOrDefaultAsync(cancellationToken);
        if (item.KnowledgeAssetId.HasValue)
            assetTitle = await _db.KnowledgeAssets
                .Where(a => a.Id == item.KnowledgeAssetId.Value)
                .Select(a => a.Title)
                .FirstOrDefaultAsync(cancellationToken);

        return new LearningPathItemDto
        {
            Id = item.Id, ItemType = item.ItemType,
            SessionId = item.SessionId, SessionTitle = sessionTitle,
            KnowledgeAssetId = item.KnowledgeAssetId, AssetTitle = assetTitle,
            OrderSequence = item.OrderSequence, IsRequired = item.IsRequired
        };
    }

    public async Task RemoveItemAsync(Guid pathId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdminOrAbove && !_currentUser.IsInRole(UserRole.KnowledgeTeam))
            throw new ForbiddenException("Only Admin or KnowledgeTeam may manage learning path content.");

        var item = await _db.LearningPathItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.LearningPathId == pathId
                && i.TenantId == _currentUser.TenantId, cancellationToken);
        if (item is null) throw new NotFoundException("LearningPathItem", itemId);

        _db.LearningPathItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateSlug(string title)
        => title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            .Trim('-');

    private static string GenerateCertificateNumber()
    {
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"CERT-{year}{month:D2}-{suffix}";
    }

    private static LearningPathDto ToDto(LearningPath path, int itemCount) => new()
    {
        Id = path.Id, Title = path.Title, Slug = path.Slug,
        Description = path.Description, Objective = path.Objective,
        CategoryId = path.CategoryId, CategoryName = null,
        DifficultyLevel = path.DifficultyLevel,
        EstimatedDurationMinutes = path.EstimatedDurationMinutes,
        IsPublished = path.IsPublished, IsAssignable = path.IsAssignable,
        CoverImageUrl = path.CoverImageUrl, ItemCount = itemCount
    };
}
