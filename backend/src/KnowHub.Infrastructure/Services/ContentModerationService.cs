using KnowHub.Application.Contracts;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services;

public class ContentModerationService : IContentModerationService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public ContentModerationService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task ReportContentAsync(ReportContentRequest request, CancellationToken ct)
    {
        if (request.TargetPostId is null && request.TargetCommentId is null)
            throw new Domain.Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["Target"] = ["Either TargetPostId or TargetCommentId must be provided."]
            });

        var tenantId = _currentUser.TenantId;
        var userId   = _currentUser.UserId;

        // Verify target exists within tenant
        if (request.TargetPostId.HasValue)
        {
            var postExists = await _db.CommunityPosts
                .AnyAsync(p => p.Id == request.TargetPostId && p.TenantId == tenantId, ct);
            if (!postExists)
                throw new NotFoundException("Post", request.TargetPostId.Value);
        }

        if (request.TargetCommentId.HasValue)
        {
            var commentExists = await _db.PostComments
                .AnyAsync(c => c.Id == request.TargetCommentId && c.TenantId == tenantId, ct);
            if (!commentExists)
                throw new NotFoundException("Comment", request.TargetCommentId.Value);
        }

        // Prevent duplicate active reports from the same user
        var duplicateExists = await _db.ContentReports
            .AnyAsync(r => r.TenantId == tenantId
                           && r.ReporterId == userId
                           && r.TargetPostId == request.TargetPostId
                           && r.TargetCommentId == request.TargetCommentId
                           && r.Status == ReportStatus.Open, ct);

        if (duplicateExists) return; // idempotent — silently ignore duplicate

        var report = new ContentReport
        {
            TenantId        = tenantId,
            ReporterId      = userId,
            TargetPostId    = request.TargetPostId,
            TargetCommentId = request.TargetCommentId,
            ReasonCode      = request.ReasonCode,
            Description     = request.Description,
            Status          = ReportStatus.Open,
            CreatedBy       = userId,
            ModifiedBy      = userId
        };

        _db.ContentReports.Add(report);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<ContentReportDto>> GetOpenReportsAsync(int pageNumber, int pageSize, CancellationToken ct)
    {
        pageSize   = Math.Clamp(pageSize, 1, 100);
        pageNumber = Math.Max(1, pageNumber);

        var tenantId = _currentUser.TenantId;

        var query = _db.ContentReports
            .Where(r => r.TenantId == tenantId && r.Status == ReportStatus.Open);

        var total = await query.CountAsync(ct);
        var reports = await query
            .OrderBy(r => r.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.Reporter)
            .Include(r => r.TargetPost)
            .Include(r => r.Resolver)
            .AsNoTracking()
            .Select(r => new ContentReportDto
            {
                Id               = r.Id,
                ReporterName     = r.Reporter!.FullName,
                TargetPostId     = r.TargetPostId,
                TargetPostTitle  = r.TargetPost != null ? r.TargetPost.Title : null,
                TargetCommentId  = r.TargetCommentId,
                ReasonCode       = r.ReasonCode,
                Description      = r.Description,
                Status           = r.Status,
                ResolverName     = r.Resolver != null ? r.Resolver.FullName : null,
                ResolvedAt       = r.ResolvedAt,
                CreatedDate      = r.CreatedDate
            })
            .ToListAsync(ct);

        return new PagedResult<ContentReportDto> { Data = reports, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task ResolveReportAsync(Guid reportId, ResolveReportRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId   = _currentUser.UserId;

        var report = await _db.ContentReports
            .Where(r => r.Id == reportId && r.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("ContentReport", reportId);

        if (report.Status != ReportStatus.Open)
            throw new InvalidOperationException("Report is already closed.");

        report.Status     = ReportStatus.Resolved;
        report.ResolvedBy = userId;
        report.ResolvedAt = DateTime.UtcNow;
        report.ModifiedBy = userId;
        report.ModifiedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DismissReportAsync(Guid reportId, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        var userId   = _currentUser.UserId;

        var report = await _db.ContentReports
            .Where(r => r.Id == reportId && r.TenantId == tenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("ContentReport", reportId);

        if (report.Status != ReportStatus.Open)
            throw new InvalidOperationException("Report is already closed.");

        report.Status     = ReportStatus.Dismissed;
        report.ResolvedBy = userId;
        report.ResolvedAt = DateTime.UtcNow;
        report.ModifiedBy = userId;
        report.ModifiedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}

