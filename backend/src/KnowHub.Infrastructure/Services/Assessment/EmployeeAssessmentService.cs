using System.Text.Json;
using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Assessment;
using KnowHub.Application.Models;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services.Assessment;

public class EmployeeAssessmentService : IEmployeeAssessmentService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public EmployeeAssessmentService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<EmployeeAssessmentDto>> GetAssessmentsAsync(AssessmentFilter filter, CancellationToken ct)
    {
        var query = _db.EmployeeAssessments
            .Where(a => a.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (filter.GroupId.HasValue)  query = query.Where(a => a.GroupId == filter.GroupId.Value);
        if (filter.PeriodId.HasValue) query = query.Where(a => a.AssessmentPeriodId == filter.PeriodId.Value);
        if (filter.Status.HasValue)   query = query.Where(a => (int)a.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(a => a.Employee.FullName.Contains(filter.Search));

        var (data, total) = await query
            .OrderBy(a => a.Employee.FullName)
            .Select(a => new EmployeeAssessmentDto
            {
                Id                 = a.Id,
                UserId             = a.UserId,
                EmployeeName       = a.Employee.FullName,
                Department         = a.Employee.Department,
                Designation        = a.Employee.Designation,
                GroupId            = a.GroupId,
                GroupName          = a.Group.GroupName,
                AssessmentPeriodId = a.AssessmentPeriodId,
                PeriodName         = a.Period.Name,
                RatingScaleId      = a.RatingScaleId,
                RatingScaleName    = a.RatingScale.Name,
                RatingValue        = a.RatingValue,
                Comment            = a.Comment,
                EvidenceNotes      = a.EvidenceNotes,
                Status             = a.Status,
                SubmittedOn        = a.SubmittedOn,
                RecordVersion      = a.RecordVersion
            })
            .ToPagedListAsync(filter.PageNumber, filter.PageSize, ct);

        return new PagedResult<EmployeeAssessmentDto> { Data = data, TotalCount = total, PageNumber = filter.PageNumber, PageSize = filter.PageSize };
    }

    public async Task<EmployeeAssessmentDto> GetAssessmentByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.EmployeeAssessments
            .Where(a => a.Id == id && a.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .Select(a => new EmployeeAssessmentDto
            {
                Id                 = a.Id,
                UserId             = a.UserId,
                EmployeeName       = a.Employee.FullName,
                Department         = a.Employee.Department,
                Designation        = a.Employee.Designation,
                GroupId            = a.GroupId,
                GroupName          = a.Group.GroupName,
                AssessmentPeriodId = a.AssessmentPeriodId,
                PeriodName         = a.Period.Name,
                RatingScaleId      = a.RatingScaleId,
                RatingScaleName    = a.RatingScale.Name,
                RatingValue        = a.RatingValue,
                Comment            = a.Comment,
                EvidenceNotes      = a.EvidenceNotes,
                Status             = a.Status,
                SubmittedOn        = a.SubmittedOn,
                RecordVersion      = a.RecordVersion
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("EmployeeAssessment", id);
    }

    public async Task<List<EmployeeAssessmentDto>> GetOrCreateDraftsForPeriodAsync(AssessmentGridRequest request, CancellationToken ct)
    {
        var period = await _db.AssessmentPeriods
            .FirstOrDefaultAsync(p => p.Id == request.PeriodId && p.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("AssessmentPeriod", request.PeriodId);

        if (period.Status != AssessmentPeriodStatus.Open)
            throw new BusinessRuleException("The period is not open for assessment entry.");

        // Ensure Draft records exist for all active group employees
        var members = await _db.AssessmentGroupMembers
            .Where(e => e.GroupId == request.GroupId && e.IsActive && e.Group.TenantId == _currentUser.TenantId)
            .Select(e => new { e.UserId, RoleCode = e.WorkRole != null ? e.WorkRole.Code : string.Empty })
            .ToListAsync(ct);

        var employeeIds = members.Select(m => m.UserId).ToList();

        var existingIds = await _db.EmployeeAssessments
            .Where(a => a.AssessmentPeriodId == request.PeriodId && a.GroupId == request.GroupId
                     && a.TenantId == _currentUser.TenantId)
            .Select(a => a.UserId)
            .ToListAsync(ct);

        var defaultScale = await _db.RatingScales
            .Where(s => s.TenantId == _currentUser.TenantId && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .FirstOrDefaultAsync(ct);

        foreach (var emp in members.Where(m => !existingIds.Contains(m.UserId)))
        {
            _db.EmployeeAssessments.Add(new EmployeeAssessment
            {
                RoleCode           = emp.RoleCode,
                TenantId           = _currentUser.TenantId,
                UserId             = emp.UserId,
                GroupId            = request.GroupId,
                AssessmentPeriodId = request.PeriodId,
                RatingScaleId      = defaultScale?.Id ?? Guid.Empty,
                RatingValue        = 0,
                Status             = AssessmentStatus.Draft,
                CreatedBy          = _currentUser.UserId,
                ModifiedBy         = _currentUser.UserId
            });
        }

        if (members.Any(m => !existingIds.Contains(m.UserId)))
            await _db.SaveChangesAsync(ct);

        var query = _db.EmployeeAssessments
            .Where(a => a.AssessmentPeriodId == request.PeriodId && a.GroupId == request.GroupId
                     && a.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(a => a.Employee.FullName.Contains(request.Search));
        if (request.Rating.HasValue)
            query = query.Where(a => a.RatingValue == request.Rating.Value);
        if (request.Status.HasValue)
            query = query.Where(a => (int)a.Status == request.Status.Value);

        return await query
            .OrderBy(a => a.Employee.FullName)
            .Select(a => new EmployeeAssessmentDto
            {
                Id                 = a.Id,
                UserId             = a.UserId,
                EmployeeName       = a.Employee.FullName,
                Department         = a.Employee.Department,
                Designation        = a.Employee.Designation,
                GroupId            = a.GroupId,
                GroupName          = a.Group.GroupName,
                AssessmentPeriodId = a.AssessmentPeriodId,
                PeriodName         = a.Period.Name,
                RatingScaleId      = a.RatingScaleId,
                RatingScaleName    = a.RatingScale.Name,
                RatingValue        = a.RatingValue,
                Comment            = a.Comment,
                EvidenceNotes      = a.EvidenceNotes,
                Status             = a.Status,
                SubmittedOn        = a.SubmittedOn,
                RecordVersion      = a.RecordVersion
            })
            .ToListAsync(ct);
    }

    public async Task<EmployeeAssessmentDto> SaveDraftAsync(SaveAssessmentDraftRequest request, CancellationToken ct)
    {
        var period = await _db.AssessmentPeriods
            .FirstOrDefaultAsync(p => p.Id == request.AssessmentPeriodId && p.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("AssessmentPeriod", request.AssessmentPeriodId);

        if (period.Status != AssessmentPeriodStatus.Open)
            throw new BusinessRuleException("Ratings can only be saved when the period is Open.");

        var isChampion = await IsPrimaryLeadOfGroupAsync(request.GroupId, ct);
        if (!_currentUser.IsAdminOrAbove && !isChampion)
            throw new ForbiddenException("Only the group Primary Lead or Admin can save ratings.");

        var existing = await _db.EmployeeAssessments
            .FirstOrDefaultAsync(a => a.UserId == request.UserId
                                   && a.GroupId == request.GroupId
                                   && a.AssessmentPeriodId == request.AssessmentPeriodId
                                   && a.TenantId == _currentUser.TenantId, ct);

        if (existing is not null)
        {
            if (existing.Status == AssessmentStatus.Submitted)
                throw new BusinessRuleException("A submitted assessment cannot be overwritten. Use Reopen first.");

            var oldJson = JsonSerializer.Serialize(new { existing.RatingValue, existing.RatingScaleId, existing.Comment });
            existing.RatingScaleId  = request.RatingScaleId;
            existing.RatingValue    = request.RatingValue;
            existing.Comment        = request.Comment;
            existing.EvidenceNotes  = request.EvidenceNotes;
            existing.ModifiedOn     = DateTime.UtcNow;
            existing.ModifiedBy     = _currentUser.UserId;
            existing.RecordVersion++;

            WriteAudit(existing.Id, existing.Id, "EmployeeAssessment", AssessmentActionType.Updated, oldJson, null);
            await _db.SaveChangesAsync(ct);
            return await GetAssessmentByIdAsync(existing.Id, ct);
        }

        var entity = new EmployeeAssessment
        {
            TenantId           = _currentUser.TenantId,
            UserId             = request.UserId,
            GroupId            = request.GroupId,
            AssessmentPeriodId = request.AssessmentPeriodId,
            RatingScaleId      = request.RatingScaleId,
            RatingValue        = request.RatingValue,
            Comment            = request.Comment,
            EvidenceNotes      = request.EvidenceNotes,
            Status             = AssessmentStatus.Draft,
            CreatedBy          = _currentUser.UserId,
            ModifiedBy         = _currentUser.UserId
        };
        _db.EmployeeAssessments.Add(entity);
        WriteAudit(entity.Id, entity.Id, "EmployeeAssessment", AssessmentActionType.Created, null, null);
        await _db.SaveChangesAsync(ct);

        return await GetAssessmentByIdAsync(entity.Id, ct);
    }

    public async Task<List<EmployeeAssessmentDto>> BulkSaveDraftsAsync(BulkSaveAssessmentRequest request, CancellationToken ct)
    {
        var results = new List<EmployeeAssessmentDto>();
        foreach (var item in request.Assessments)
        {
            item.AssessmentPeriodId = request.AssessmentPeriodId;
            item.GroupId             = request.GroupId;
            results.Add(await SaveDraftAsync(item, ct));
        }
        return results;
    }

    public async Task SubmitAssessmentAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.EmployeeAssessments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("EmployeeAssessment", id);

        if (entity.Status != AssessmentStatus.Draft && entity.Status != AssessmentStatus.Reopened)
            throw new BusinessRuleException("Only Draft or Reopened assessments can be submitted.");

        var period = await _db.AssessmentPeriods.FindAsync(new object[] { entity.AssessmentPeriodId }, ct)
            ?? throw new NotFoundException("AssessmentPeriod", entity.AssessmentPeriodId);

        if (period.Status != AssessmentPeriodStatus.Open)
            throw new BusinessRuleException("The period is not open.");

        var isChampion = await IsPrimaryLeadOfGroupAsync(entity.GroupId, ct);
        if (!_currentUser.IsAdminOrAbove && !isChampion)
            throw new ForbiddenException("Only the group Primary Lead or Admin can submit assessments.");

        entity.Status      = AssessmentStatus.Submitted;
        entity.SubmittedOn = DateTime.UtcNow;
        entity.ModifiedOn  = DateTime.UtcNow;
        entity.ModifiedBy  = _currentUser.UserId;
        entity.RecordVersion++;

        WriteAudit(entity.Id, entity.Id, "EmployeeAssessment", AssessmentActionType.Submitted, null, null);
        await _db.SaveChangesAsync(ct);
    }

    public async Task BulkSubmitAsync(BulkSubmitRequest request, CancellationToken ct)
    {
        var period = await _db.AssessmentPeriods
            .FirstOrDefaultAsync(p => p.Id == request.PeriodId && p.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("AssessmentPeriod", request.PeriodId);

        if (period.Status != AssessmentPeriodStatus.Open)
            throw new BusinessRuleException("The period is not open.");

        var isChampion = await IsPrimaryLeadOfGroupAsync(request.GroupId, ct);
        if (!_currentUser.IsAdminOrAbove && !isChampion)
            throw new ForbiddenException("Only the group Primary Lead or Admin can bulk submit.");

        var drafts = await _db.EmployeeAssessments
            .Where(a => a.GroupId == request.GroupId
                     && a.AssessmentPeriodId == request.PeriodId
                     && (a.Status == AssessmentStatus.Draft || a.Status == AssessmentStatus.Reopened)
                     && a.TenantId == _currentUser.TenantId)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var a in drafts)
        {
            a.Status      = AssessmentStatus.Submitted;
            a.SubmittedOn = now;
            a.ModifiedOn  = now;
            a.ModifiedBy  = _currentUser.UserId;
            a.RecordVersion++;
            WriteAudit(a.Id, a.Id, "EmployeeAssessment", AssessmentActionType.Submitted, null, null);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task ReopenAssessmentAsync(Guid id, ReopenAssessmentRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can reopen assessments.");

        var entity = await _db.EmployeeAssessments
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("EmployeeAssessment", id);

        if (entity.Status != AssessmentStatus.Submitted)
            throw new BusinessRuleException("Only submitted assessments can be reopened.");

        entity.Status     = AssessmentStatus.Reopened;
        entity.ModifiedOn = DateTime.UtcNow;
        entity.ModifiedBy = _currentUser.UserId;
        entity.RecordVersion++;

        WriteAudit(entity.Id, entity.Id, "EmployeeAssessment", AssessmentActionType.Reopened,
            null, JsonSerializer.Serialize(new { request.Remarks }));

        await _db.SaveChangesAsync(ct);
    }

    // -- Dashboard ------------------------------------------------------------

    public async Task<PrimaryLeadDashboardDto> GetPrimaryLeadDashboardAsync(Guid groupId, Guid? periodId, CancellationToken ct)
    {
        var activePeriod = await ResolvePeriodAsync(periodId, ct);
        if (activePeriod is null)
            return new PrimaryLeadDashboardDto { GroupName = "", PeriodName = "No open period" };

        var group = await _db.AssessmentGroups
            .Where(g => g.Id == groupId && g.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .Select(g => new { g.GroupName })
            .FirstOrDefaultAsync(ct);

        var total = await _db.AssessmentGroupMembers
            .CountAsync(e => e.GroupId == groupId && e.IsActive && e.Group.TenantId == _currentUser.TenantId, ct);

        var submitted = await _db.EmployeeAssessments
            .CountAsync(a => a.GroupId == groupId && a.AssessmentPeriodId == activePeriod.Id
                          && a.Status == AssessmentStatus.Submitted && a.TenantId == _currentUser.TenantId, ct);

        var distribution = await GetRatingDistributionAsync(groupId, activePeriod.Id, ct);

        return new PrimaryLeadDashboardDto
        {
            TotalEmployees            = total,
            Rated                     = submitted,
            Pending                   = total - submitted,
            CompletionPercent         = total > 0 ? Math.Round((decimal)submitted / total * 100, 1) : 0,
            CurrentRatingDistribution = distribution,
            GroupName                 = group?.GroupName ?? "",
            PeriodName                = activePeriod.Name
        };
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(Guid? periodId, CancellationToken ct)
    {
        var activePeriod = await ResolvePeriodAsync(periodId, ct);
        if (activePeriod is null)
            return new AdminDashboardDto { PeriodName = "No open period" };

        var groups = await _db.AssessmentGroups
            .Where(g => g.TenantId == _currentUser.TenantId && g.IsActive)
            .AsNoTracking()
            .Select(g => new
            {
                g.Id, g.GroupName, PrimaryLeadName = g.PrimaryLead.FullName,
                Total     = g.GroupMembers.Count(e => e.IsActive),
                Submitted = g.GroupMembers.Count(e => e.IsActive && g.Assessments
                    .Any(a => a.UserId == e.UserId && a.AssessmentPeriodId == activePeriod.Id
                           && a.Status == AssessmentStatus.Submitted))
            })
            .ToListAsync(ct);

        var completion = groups.Select(g => new GroupCompletionRow
        {
            GroupId           = g.Id,
            GroupName         = g.GroupName,
            PrimaryLeadName      = g.PrimaryLeadName,
            TotalEmployees    = g.Total,
            Submitted         = g.Submitted,
            Pending           = g.Total - g.Submitted,
            CompletionPercent = g.Total > 0 ? Math.Round((decimal)g.Submitted / g.Total * 100, 1) : 0
        }).ToList();

        var ratingDist = await GetRatingDistributionAsync(null, activePeriod.Id, ct);

        return new AdminDashboardDto
        {
            TotalGroups          = groups.Count,
            TotalEmployees       = groups.Sum(g => g.Total),
            AvgCompletionPercent = completion.Count > 0 ? Math.Round(completion.Average(c => c.CompletionPercent), 1) : 0,
            PeriodName           = activePeriod.Name,
            GroupCompletions     = completion,
            RatingDistribution   = ratingDist
        };
    }

    public async Task<CoLeadDashboardDto> GetCoLeadDashboardAsync(Guid? periodId, CancellationToken ct)
    {
        var admin = await GetAdminDashboardAsync(periodId, ct);
        return new CoLeadDashboardDto
        {
            Groups             = admin.GroupCompletions,
            RatingDistribution = admin.RatingDistribution,
            PeriodName         = admin.PeriodName
        };
    }

    public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(Guid? periodId, CancellationToken ct)
    {
        var activePeriod = await ResolvePeriodAsync(periodId, ct);
        if (activePeriod is null)
            return new ExecutiveDashboardDto { PeriodName = "No open period" };

        var ratingDist = await GetRatingDistributionAsync(null, activePeriod.Id, ct);
        var totalAssessed = ratingDist.Sum(r => r.Count);
        var avgScore = totalAssessed > 0
            ? ratingDist.Sum(r => (decimal)r.NumericValue * r.Count) / totalAssessed
            : 0;

        var trend = await _db.AssessmentPeriods
            .Where(p => p.TenantId == _currentUser.TenantId && p.Status == AssessmentPeriodStatus.Published)
            .OrderByDescending(p => p.StartDate)
            .Take(10)
            .Select(p => new PeriodTrendRow
            {
                PeriodName = p.Name,
                AvgScore   = _db.EmployeeAssessments
                    .Where(a => a.AssessmentPeriodId == p.Id && a.Status == AssessmentStatus.Submitted
                             && a.TenantId == _currentUser.TenantId)
                    .Average(a => (decimal?)a.RatingValue) ?? 0,
                TotalRated = _db.EmployeeAssessments
                    .Count(a => a.AssessmentPeriodId == p.Id && a.Status == AssessmentStatus.Submitted
                             && a.TenantId == _currentUser.TenantId)
            })
            .ToListAsync(ct);

        return new ExecutiveDashboardDto
        {
            OrgMaturityScore   = Math.Round(avgScore, 2),
            TotalAssessed      = totalAssessed,
            PeriodName         = activePeriod.Name,
            RatingDistribution = ratingDist,
            HistoricalTrend    = trend
        };
    }

    // -- private helpers ------------------------------------------------------

    private async Task<AssessmentPeriod?> ResolvePeriodAsync(Guid? periodId, CancellationToken ct)
    {
        if (periodId.HasValue)
            return await _db.AssessmentPeriods
                .FirstOrDefaultAsync(p => p.Id == periodId.Value && p.TenantId == _currentUser.TenantId, ct);

        return await _db.AssessmentPeriods
            .Where(p => p.Status == AssessmentPeriodStatus.Open && p.TenantId == _currentUser.TenantId)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<List<RatingBandCount>> GetRatingDistributionAsync(Guid? groupId, Guid periodId, CancellationToken ct)
    {
        var query = _db.EmployeeAssessments
            .Where(a => a.AssessmentPeriodId == periodId && a.Status == AssessmentStatus.Submitted
                     && a.TenantId == _currentUser.TenantId);

        if (groupId.HasValue)
            query = query.Where(a => a.GroupId == groupId.Value);

        return await query
            .GroupBy(a => new { a.RatingScale.Name, a.RatingScale.NumericValue })
            .Select(g => new RatingBandCount
            {
                RatingName   = g.Key.Name,
                NumericValue = g.Key.NumericValue,
                Count        = g.Count()
            })
            .OrderBy(r => r.NumericValue)
            .ToListAsync(ct);
    }

    private async Task<bool> IsPrimaryLeadOfGroupAsync(Guid groupId, CancellationToken ct)
    {
        return await _db.AssessmentGroups
            .AnyAsync(g => g.Id == groupId && g.PrimaryLeadUserId == _currentUser.UserId
                        && g.IsActive && g.TenantId == _currentUser.TenantId, ct);
    }

    private void WriteAudit(Guid? assessmentId, Guid entityId, string entityType,
        AssessmentActionType action, string? oldJson, string? newJson)
    {
        _db.AssessmentAuditLogs.Add(new AssessmentAuditLog
        {
            TenantId             = _currentUser.TenantId,
            EmployeeAssessmentId = assessmentId,
            RelatedEntityType    = entityType,
            RelatedEntityId      = entityId,
            ActionType           = action,
            OldValueJson         = oldJson,
            NewValueJson         = newJson,
            ChangedBy            = _currentUser.UserId,
            ChangedOn            = DateTime.UtcNow,
            CreatedBy            = _currentUser.UserId,
            ModifiedBy           = _currentUser.UserId
        });
    }
}
