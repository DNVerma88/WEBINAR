using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Assessment;
using KnowHub.Application.Models;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services.Assessment;

public class AssessmentAuditService : IAssessmentAuditService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public AssessmentAuditService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<AssessmentAuditLogDto>> GetAuditLogsAsync(AuditFilter filter, CancellationToken ct)
    {
        var query = _db.AssessmentAuditLogs
            .Where(l => l.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (filter.EntityId.HasValue)   query = query.Where(l => l.RelatedEntityId == filter.EntityId.Value);
        if (filter.ActionType.HasValue) query = query.Where(l => (int)l.ActionType == filter.ActionType.Value);
        if (filter.ChangedBy.HasValue)  query = query.Where(l => l.ChangedBy == filter.ChangedBy.Value);
        if (!string.IsNullOrWhiteSpace(filter.ChangedByName))
            query = query.Where(l => l.ChangedByUser.FullName.Contains(filter.ChangedByName));
        if (filter.From.HasValue)       query = query.Where(l => l.ChangedOn >= filter.From.Value);
        if (filter.To.HasValue)         query = query.Where(l => l.ChangedOn <= filter.To.Value);

        var (data, total) = await query
            .OrderByDescending(l => l.ChangedOn)
            .Select(l => new AssessmentAuditLogDto
            {
                Id                   = l.Id,
                EmployeeAssessmentId = l.EmployeeAssessmentId,
                RelatedEntityType    = l.RelatedEntityType,
                RelatedEntityId      = l.RelatedEntityId,
                ActionType           = l.ActionType.ToString(),
                OldValueJson         = l.OldValueJson,
                NewValueJson         = l.NewValueJson,
                ChangedBy            = l.ChangedBy,
                ChangedByName        = l.ChangedByUser.FullName,
                ChangedOn            = l.ChangedOn,
                Remarks              = l.Remarks
            })
            .ToPagedListAsync(filter.PageNumber, filter.PageSize, ct);

        return new PagedResult<AssessmentAuditLogDto> { Data = data, TotalCount = total, PageNumber = filter.PageNumber, PageSize = filter.PageSize };
    }
}
