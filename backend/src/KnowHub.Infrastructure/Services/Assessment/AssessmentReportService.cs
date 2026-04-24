using System.Text;
using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Assessment;
using KnowHub.Application.Models;
using KnowHub.Domain.Enums;
using KnowHub.Infrastructure.Persistence;
using KnowHub.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Services.Assessment;

public class AssessmentReportService : IAssessmentReportService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public AssessmentReportService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<DetailedAssessmentReportRow>> GetDetailedReportAsync(DetailedReportFilter filter, CancellationToken ct)
    {
        var query = _db.EmployeeAssessments
            .Where(a => a.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (filter.PeriodId.HasValue)    query = query.Where(a => a.AssessmentPeriodId == filter.PeriodId.Value);
        if (filter.GroupId.HasValue)     query = query.Where(a => a.GroupId == filter.GroupId.Value);
        if (filter.PrimaryLeadId.HasValue)  query = query.Where(a => a.Group.PrimaryLeadUserId == filter.PrimaryLeadId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Designation))
            query = query.Where(a => a.Employee.Designation == filter.Designation);
        if (!string.IsNullOrWhiteSpace(filter.Department))
            query = query.Where(a => a.Employee.Department == filter.Department);
        if (filter.Rating.HasValue)      query = query.Where(a => a.RatingValue == filter.Rating.Value);
        if (filter.Status.HasValue)      query = query.Where(a => (int)a.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(a => a.Employee.FullName.Contains(filter.Search));

        var (data, total) = await query
            .OrderBy(a => a.Employee.FullName)
            .Select(a => new DetailedAssessmentReportRow
            {
                EmployeeName        = a.Employee.FullName,
                Designation         = a.Employee.Designation,
                Department          = a.Employee.Department,
                GroupName           = a.Group.GroupName,
                PrimaryLeadName        = a.Group.PrimaryLead.FullName,
                PeriodName          = a.Period.Name,
                CurrentRatingValue  = a.RatingValue,
                CurrentRatingName   = a.RatingScale.Name,
                Comment             = a.Comment,
                EvidenceNotes       = a.EvidenceNotes,
                Status              = a.Status.ToString()
            })
            .ToPagedListAsync(filter.PageNumber, filter.PageSize, ct);

        return new PagedResult<DetailedAssessmentReportRow> { Data = data, TotalCount = total, PageNumber = filter.PageNumber, PageSize = filter.PageSize };
    }

    public async Task<CompletionReportDto> GetCompletionReportAsync(Guid? periodId, Guid? groupId, CancellationToken ct)
    {
        var query = _db.EmployeeAssessments
            .Where(a => a.TenantId == _currentUser.TenantId);

        if (periodId.HasValue) query = query.Where(a => a.AssessmentPeriodId == periodId.Value);
        if (groupId.HasValue)  query = query.Where(a => a.GroupId == groupId.Value);

        var total     = await query.CountAsync(ct);
        var completed = await query.CountAsync(a => a.Status == AssessmentStatus.Submitted, ct);

        var byGroup = await _db.AssessmentGroups
            .Where(g => g.TenantId == _currentUser.TenantId && g.IsActive)
            .AsNoTracking()
            .Select(g => new GroupCompletionRow
            {
                GroupId           = g.Id,
                GroupName         = g.GroupName,
                PrimaryLeadName      = g.PrimaryLead.FullName,
                TotalEmployees    = g.GroupMembers.Count(e => e.IsActive),
                Submitted         = g.Assessments
                    .Count(a => a.Status == AssessmentStatus.Submitted
                             && (!periodId.HasValue || a.AssessmentPeriodId == periodId.Value)),
                Pending           = g.GroupMembers.Count(e => e.IsActive) - g.Assessments
                    .Count(a => a.Status == AssessmentStatus.Submitted
                             && (!periodId.HasValue || a.AssessmentPeriodId == periodId.Value)),
                CompletionPercent = g.GroupMembers.Count(e => e.IsActive) > 0
                    ? Math.Round((decimal)g.Assessments.Count(a =>
                        a.Status == AssessmentStatus.Submitted
                        && (!periodId.HasValue || a.AssessmentPeriodId == periodId.Value))
                        / g.GroupMembers.Count(e => e.IsActive) * 100, 1)
                    : 0
            })
            .ToListAsync(ct);

        return new CompletionReportDto
        {
            TotalExpected = total,
            Completed     = completed,
            Pending       = total - completed,
            CompletionPct = total > 0 ? Math.Round((decimal)completed / total * 100, 1) : 0,
            ByGroup       = byGroup
        };
    }

    public async Task<List<GroupDistributionRow>> GetGroupDistributionAsync(Guid periodId, CancellationToken ct)
    {
        var groups = await _db.AssessmentGroups
            .Where(g => g.TenantId == _currentUser.TenantId && g.IsActive)
            .AsNoTracking()
            .Select(g => g.GroupName)
            .ToListAsync(ct);

        var result = new List<GroupDistributionRow>();
        foreach (var groupName in groups)
        {
            var dist = await _db.EmployeeAssessments
                .Where(a => a.AssessmentPeriodId == periodId && a.Group.GroupName == groupName
                         && a.Status == AssessmentStatus.Submitted && a.TenantId == _currentUser.TenantId)
                .AsNoTracking()
                .GroupBy(a => new { a.RatingScale.Name, a.RatingScale.NumericValue })
                .Select(g => new RatingBandCount
                {
                    RatingName   = g.Key.Name,
                    NumericValue = g.Key.NumericValue,
                    Count        = g.Count()
                })
                .OrderBy(r => r.NumericValue)
                .ToListAsync(ct);

            result.Add(new GroupDistributionRow { GroupName = groupName, Distribution = dist });
        }
        return result;
    }

    public async Task<List<RoleDistributionRow>> GetRoleDistributionAsync(Guid periodId, CancellationToken ct)
    {
        return await _db.EmployeeAssessments
            .Where(a => a.AssessmentPeriodId == periodId && a.Status == AssessmentStatus.Submitted
                     && a.TenantId == _currentUser.TenantId && a.Employee.Designation != null)
            .AsNoTracking()
            .GroupBy(a => a.Employee.Designation!)
            .Select(g => new RoleDistributionRow
            {
                Designation = g.Key,
                AvgScore    = Math.Round(g.Average(a => (decimal)a.RatingValue), 2)
            })
            .OrderBy(r => r.Designation)
            .ToListAsync(ct);
    }

    public async Task<List<EmployeeHistoryRow>> GetEmployeeHistoryAsync(Guid userId, CancellationToken ct)
    {
        return await _db.EmployeeAssessments
            .Where(a => a.UserId == userId && a.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .OrderByDescending(a => a.Period.StartDate)
            .Select(a => new EmployeeHistoryRow
            {
                PeriodName  = a.Period.Name,
                RatingValue = a.RatingValue,
                RatingName  = a.RatingScale.Name,
                Comment     = a.Comment,
                Status      = a.Status.ToString(),
                StartDate   = a.Period.StartDate
            })
            .ToListAsync(ct);
    }

    public async Task<TrendReportDto> GetTrendReportAsync(TrendReportFilter filter, CancellationToken ct)
    {
        var query = _db.AssessmentPeriods
            .Where(p => p.TenantId == _currentUser.TenantId && p.Year >= filter.FromYear && p.Year <= filter.ToYear)
            .AsNoTracking();

        var periods = await query
            .OrderBy(p => p.StartDate)
            .Select(p => new PeriodTrendRow
            {
                PeriodName = p.Name,
                AvgScore   = _db.EmployeeAssessments
                    .Where(a => a.AssessmentPeriodId == p.Id && a.Status == AssessmentStatus.Submitted
                             && a.TenantId == _currentUser.TenantId
                             && (!filter.GroupId.HasValue || a.GroupId == filter.GroupId.Value))
                    .Average(a => (decimal?)a.RatingValue) ?? 0,
                TotalRated = _db.EmployeeAssessments
                    .Count(a => a.AssessmentPeriodId == p.Id && a.Status == AssessmentStatus.Submitted
                             && a.TenantId == _currentUser.TenantId
                             && (!filter.GroupId.HasValue || a.GroupId == filter.GroupId.Value))
            })
            .ToListAsync(ct);

        int improving = 0, declining = 0, stable = 0;
        for (int i = 1; i < periods.Count; i++)
        {
            var diff = periods[i].AvgScore - periods[i - 1].AvgScore;
            if (diff > 0.1m) improving++;
            else if (diff < -0.1m) declining++;
            else stable++;
        }

        return new TrendReportDto { Periods = periods, ImprovingCount = improving, DecliningCount = declining, StableCount = stable };
    }

    public async Task<List<GroupRiskRow>> GetRiskReportAsync(Guid periodId, CancellationToken ct)
    {
        return await _db.AssessmentGroups
            .Where(g => g.TenantId == _currentUser.TenantId && g.IsActive)
            .AsNoTracking()
            .Select(g => new GroupRiskRow
            {
                GroupName = g.GroupName,
                AvgScore  = _db.EmployeeAssessments
                    .Where(a => a.GroupId == g.Id && a.AssessmentPeriodId == periodId
                             && a.Status == AssessmentStatus.Submitted && a.TenantId == _currentUser.TenantId)
                    .Average(a => (decimal?)a.RatingValue) ?? 0,
                Missing   = g.GroupMembers.Count(e => e.IsActive) - _db.EmployeeAssessments
                    .Count(a => a.GroupId == g.Id && a.AssessmentPeriodId == periodId
                             && a.Status == AssessmentStatus.Submitted && a.TenantId == _currentUser.TenantId),
                RiskLevel = "Medium" // computed in post-processing
            })
            .OrderBy(r => r.AvgScore)
            .ToListAsync(ct);
    }

    public async Task<List<ImprovementRow>> GetImprovementReportAsync(ImprovementReportFilter filter, CancellationToken ct)
    {
        var from = await _db.EmployeeAssessments
            .Where(a => a.AssessmentPeriodId == filter.FromPeriodId && a.Status == AssessmentStatus.Submitted
                     && a.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .ToDictionaryAsync(a => a.UserId, a => a, ct);

        var to = await _db.EmployeeAssessments
            .Where(a => a.AssessmentPeriodId == filter.ToPeriodId && a.Status == AssessmentStatus.Submitted
                     && a.TenantId == _currentUser.TenantId)
            .Include(a => a.Employee)
            .Include(a => a.Group)
            .AsNoTracking()
            .ToListAsync(ct);

        return to
            .Where(a => from.TryGetValue(a.UserId, out var prev) && a.RatingValue != prev.RatingValue)
            .Select(a => new ImprovementRow
            {
                EmployeeName = a.Employee.FullName,
                GroupName    = a.Group.GroupName,
                FromRating   = from[a.UserId].RatingValue,
                ToRating     = a.RatingValue,
                Change       = a.RatingValue - from[a.UserId].RatingValue
            })
            .OrderByDescending(r => r.Change)
            .ToList();
    }

    public async Task<byte[]> ExportToCsvAsync(ExportFilter filter, CancellationToken ct)
    {
        var detailFilter = new DetailedReportFilter
        {
            PeriodId    = filter.PeriodId,
            GroupId     = filter.GroupId,
            Designation = filter.Designation,
            Department  = filter.Department,
            Rating      = filter.Rating,
            PageNumber  = 1,
            PageSize    = 10_000
        };

        var result = await GetDetailedReportAsync(detailFilter, ct);
        var sb     = new StringBuilder();
        sb.AppendLine("Employee,Designation,Department,Group,Champion,Period,CurrentRating,RatingName,Status,Comment");

        foreach (var row in result.Data)
        {
            sb.AppendLine(string.Join(",",
                CsvEsc(row.EmployeeName), CsvEsc(row.Designation), CsvEsc(row.Department),
                CsvEsc(row.GroupName), CsvEsc(row.PrimaryLeadName), CsvEsc(row.PeriodName),
                row.CurrentRatingValue, CsvEsc(row.CurrentRatingName), CsvEsc(row.Status),
                CsvEsc(row.Comment)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
    public async Task<List<WorkRoleRatingRow>> GetWorkRoleRatingReportAsync(Guid? periodId, Guid? groupId, CancellationToken ct)
    {
        var assessmentQuery = _db.EmployeeAssessments
            .Where(a => a.TenantId == _currentUser.TenantId && a.Status == AssessmentStatus.Submitted)
            .AsNoTracking();
        if (periodId.HasValue) assessmentQuery = assessmentQuery.Where(a => a.AssessmentPeriodId == periodId.Value);
        if (groupId.HasValue)  assessmentQuery = assessmentQuery.Where(a => a.GroupId == groupId.Value);

        var empQuery = _db.AssessmentGroupMembers
            .Where(e => e.Group.TenantId == _currentUser.TenantId && e.WorkRoleId.HasValue && e.IsActive)
            .AsNoTracking();
        if (groupId.HasValue) empQuery = empQuery.Where(e => e.GroupId == groupId.Value);

        var joined = await (
            from emp in empQuery
            join a in assessmentQuery on emp.UserId equals a.UserId
            where emp.GroupId == a.GroupId
            select new
            {
                emp.WorkRole!.Code,
                emp.WorkRole.Name,
                emp.WorkRole.Category,
                a.RatingValue,
                RatingScaleName  = a.RatingScale.Name,
                RatingScaleValue = a.RatingScale.NumericValue
            }
        ).ToListAsync(ct);

        return joined
            .GroupBy(x => new { x.Code, x.Name, x.Category })
            .Select(g => new WorkRoleRatingRow
            {
                WorkRoleCode = g.Key.Code,
                WorkRoleName = g.Key.Name,
                Category     = g.Key.Category,
                TotalRated   = g.Count(),
                AvgScore     = Math.Round(g.Average(x => (decimal)x.RatingValue), 2),
                Distribution = g.GroupBy(x => new { x.RatingScaleName, x.RatingScaleValue })
                                 .Select(d => new RatingBandCount
                                 {
                                     RatingName   = d.Key.RatingScaleName,
                                     NumericValue = d.Key.RatingScaleValue,
                                     Count        = d.Count()
                                 })
                                 .OrderBy(d => d.NumericValue)
                                 .ToList()
            })
            .OrderBy(r => r.Category).ThenBy(r => r.WorkRoleName)
            .ToList();
    }
    private static string CsvEsc(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
