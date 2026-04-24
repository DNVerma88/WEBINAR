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

public class AssessmentPeriodService : IAssessmentPeriodService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public AssessmentPeriodService(KnowHubDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<AssessmentPeriodDto>> GetPeriodsAsync(AssessmentPeriodFilter filter, CancellationToken ct)
    {
        var query = _db.AssessmentPeriods
            .Where(p => p.TenantId == _currentUser.TenantId)
            .AsNoTracking();

        if (filter.Year.HasValue)
            query = query.Where(p => p.Year == filter.Year.Value);

        if (filter.Status.HasValue)
            query = query.Where(p => (int)p.Status == filter.Status.Value);

        if (filter.Frequency.HasValue)
            query = query.Where(p => (int)p.Frequency == filter.Frequency.Value);

        var (data, total) = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.StartDate)
            .Select(p => new AssessmentPeriodDto
            {
                Id            = p.Id,
                Name          = p.Name,
                Frequency     = p.Frequency,
                StartDate     = p.StartDate,
                EndDate       = p.EndDate,
                Year          = p.Year,
                WeekNumber    = p.WeekNumber,
                Status        = p.Status,
                IsActive      = p.IsActive,
                RecordVersion = p.RecordVersion
            })
            .ToPagedListAsync(filter.PageNumber, filter.PageSize, ct);

        return new PagedResult<AssessmentPeriodDto> { Data = data, TotalCount = total, PageNumber = filter.PageNumber, PageSize = filter.PageSize };
    }

    public async Task<AssessmentPeriodDto> GetPeriodByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.AssessmentPeriods
            .Where(p => p.Id == id && p.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .Select(p => new AssessmentPeriodDto
            {
                Id            = p.Id,
                Name          = p.Name,
                Frequency     = p.Frequency,
                StartDate     = p.StartDate,
                EndDate       = p.EndDate,
                Year          = p.Year,
                WeekNumber    = p.WeekNumber,
                Status        = p.Status,
                IsActive      = p.IsActive,
                RecordVersion = p.RecordVersion
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("AssessmentPeriod", id);
    }

    public async Task<AssessmentPeriodDto> CreatePeriodAsync(CreateAssessmentPeriodRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can create assessment periods.");

        var entity = new AssessmentPeriod
        {
            TenantId   = _currentUser.TenantId,
            Name       = request.Name,
            Frequency  = request.Frequency,
            StartDate  = request.StartDate,
            EndDate    = request.EndDate,
            Year       = request.Year,
            WeekNumber = request.WeekNumber,
            Status     = AssessmentPeriodStatus.Draft,
            IsActive   = true,
            CreatedBy  = _currentUser.UserId,
            ModifiedBy = _currentUser.UserId
        };
        _db.AssessmentPeriods.Add(entity);

        WriteAudit(null, entity.Id, "AssessmentPeriod", AssessmentActionType.Created);
        await _db.SaveChangesAsync(ct);

        return await GetPeriodByIdAsync(entity.Id, ct);
    }

    public async Task<AssessmentPeriodDto> UpdatePeriodAsync(Guid id, UpdateAssessmentPeriodRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can update assessment periods.");

        var entity = await _db.AssessmentPeriods
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("AssessmentPeriod", id);

        if (entity.Status != AssessmentPeriodStatus.Draft)
            throw new BusinessRuleException("Only Draft periods can be edited.");

        if (entity.RecordVersion != request.RecordVersion)
            throw new ConflictException("The period has been modified by another user. Please refresh.");

        entity.Name          = request.Name;
        entity.StartDate     = request.StartDate;
        entity.EndDate       = request.EndDate;
        entity.WeekNumber    = request.WeekNumber;
        entity.ModifiedOn    = DateTime.UtcNow;
        entity.ModifiedBy    = _currentUser.UserId;
        entity.RecordVersion++;

        await _db.SaveChangesAsync(ct);
        return await GetPeriodByIdAsync(entity.Id, ct);
    }

    public async Task OpenPeriodAsync(Guid id, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can open assessment periods.");

        var entity = await _db.AssessmentPeriods
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("AssessmentPeriod", id);

        if (entity.Status != AssessmentPeriodStatus.Draft)
            throw new BusinessRuleException("Only Draft periods can be opened.");

        entity.Status     = AssessmentPeriodStatus.Open;
        entity.ModifiedOn = DateTime.UtcNow;
        entity.ModifiedBy = _currentUser.UserId;
        entity.RecordVersion++;

        WriteAudit(null, entity.Id, "AssessmentPeriod", AssessmentActionType.PeriodOpened);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ClosePeriodAsync(Guid id, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can close assessment periods.");

        var entity = await _db.AssessmentPeriods
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("AssessmentPeriod", id);

        if (entity.Status != AssessmentPeriodStatus.Open)
            throw new BusinessRuleException("Only Open periods can be closed.");

        entity.Status     = AssessmentPeriodStatus.Closed;
        entity.ModifiedOn = DateTime.UtcNow;
        entity.ModifiedBy = _currentUser.UserId;
        entity.RecordVersion++;

        WriteAudit(null, entity.Id, "AssessmentPeriod", AssessmentActionType.PeriodClosed);
        await _db.SaveChangesAsync(ct);
    }

    public async Task PublishPeriodAsync(Guid id, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can publish assessment periods.");

        var entity = await _db.AssessmentPeriods
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId, ct)
            ?? throw new NotFoundException("AssessmentPeriod", id);

        if (entity.Status != AssessmentPeriodStatus.Closed)
            throw new BusinessRuleException("Only Closed periods can be published.");

        entity.Status     = AssessmentPeriodStatus.Published;
        entity.ModifiedOn = DateTime.UtcNow;
        entity.ModifiedBy = _currentUser.UserId;
        entity.RecordVersion++;

        WriteAudit(null, entity.Id, "AssessmentPeriod", AssessmentActionType.PeriodPublished);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<AssessmentPeriodDto>> GeneratePeriodsAsync(GeneratePeriodsRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAbove)
            throw new ForbiddenException("Only Admin or SuperAdmin can generate assessment periods.");

        var existing = await _db.AssessmentPeriods
            .Where(p => p.Year == request.Year && p.Frequency == request.Frequency && p.TenantId == _currentUser.TenantId)
            .AnyAsync(ct);

        if (existing)
            throw new ConflictException($"Periods for year {request.Year} with frequency {request.Frequency} already exist.");

        var periods = request.Frequency switch
        {
            AssessmentPeriodFrequency.Weekly     => BuildWeeklyPeriods(request.Year),
            AssessmentPeriodFrequency.BiWeekly   => BuildBiWeeklyPeriods(request.Year),
            AssessmentPeriodFrequency.Monthly    => BuildMonthlyPeriods(request.Year),
            AssessmentPeriodFrequency.Quarterly  => BuildQuarterlyPeriods(request.Year),
            AssessmentPeriodFrequency.HalfYearly => BuildHalfYearlyPeriods(request.Year),
            AssessmentPeriodFrequency.Annual     => BuildAnnualPeriods(request.Year),
            _ => throw new BusinessRuleException($"Unknown frequency: {request.Frequency}")
        };

        foreach (var p in periods)
        {
            p.TenantId  = _currentUser.TenantId;
            p.CreatedBy = _currentUser.UserId;
            p.ModifiedBy = _currentUser.UserId;
        }

        _db.AssessmentPeriods.AddRange(periods);
        await _db.SaveChangesAsync(ct);

        return await _db.AssessmentPeriods
            .Where(p => p.Year == request.Year && p.Frequency == request.Frequency && p.TenantId == _currentUser.TenantId)
            .AsNoTracking()
            .OrderBy(p => p.StartDate)
            .Select(p => new AssessmentPeriodDto
            {
                Id = p.Id, Name = p.Name, Frequency = p.Frequency, StartDate = p.StartDate,
                EndDate = p.EndDate, Year = p.Year, WeekNumber = p.WeekNumber, Status = p.Status,
                IsActive = p.IsActive, RecordVersion = p.RecordVersion
            })
            .ToListAsync(ct);
    }

    private static List<AssessmentPeriod> BuildWeeklyPeriods(int year)
    {
        var list = new List<AssessmentPeriod>();
        var jan1 = new DateOnly(year, 1, 1);
        var current = jan1.DayOfWeek == DayOfWeek.Monday ? jan1 : jan1.AddDays((7 - (int)jan1.DayOfWeek + 1) % 7);
        int weekNo = 1;
        while (current.Year == year)
        {
            var end = current.AddDays(6);
            list.Add(new AssessmentPeriod
            {
                Name       = $"Week {weekNo:D2} – {year}",
                Frequency  = AssessmentPeriodFrequency.Weekly,
                StartDate  = current,
                EndDate    = end,
                Year       = year,
                WeekNumber = weekNo,
                Status     = AssessmentPeriodStatus.Draft,
                IsActive   = true
            });
            current = current.AddDays(7);
            weekNo++;
        }
        return list;
    }

    private static List<AssessmentPeriod> BuildBiWeeklyPeriods(int year)
    {
        var list = new List<AssessmentPeriod>();
        var jan1 = new DateOnly(year, 1, 1);
        var current = jan1.DayOfWeek == DayOfWeek.Monday ? jan1 : jan1.AddDays((7 - (int)jan1.DayOfWeek + 1) % 7);
        int periodNo = 1;
        while (current.Year == year)
        {
            var end = current.AddDays(13);
            list.Add(new AssessmentPeriod
            {
                Name       = $"Bi-Week {periodNo:D2} – {year}",
                Frequency  = AssessmentPeriodFrequency.BiWeekly,
                StartDate  = current,
                EndDate    = end,
                Year       = year,
                WeekNumber = null,
                Status     = AssessmentPeriodStatus.Draft,
                IsActive   = true
            });
            current = current.AddDays(14);
            periodNo++;
        }
        return list;
    }

    private void WriteAudit(Guid? assessmentId, Guid entityId, string entityType, AssessmentActionType action)
    {
        _db.AssessmentAuditLogs.Add(new AssessmentAuditLog
        {
            TenantId             = _currentUser.TenantId,
            EmployeeAssessmentId = assessmentId,
            RelatedEntityType    = entityType,
            RelatedEntityId      = entityId,
            ActionType           = action,
            ChangedBy            = _currentUser.UserId,
            ChangedOn            = DateTime.UtcNow,
            CreatedBy            = _currentUser.UserId,
            ModifiedBy           = _currentUser.UserId
        });
    }

    private static List<AssessmentPeriod> BuildMonthlyPeriods(int year)
    {
        var list = new List<AssessmentPeriod>();
        for (int month = 1; month <= 12; month++)
        {
            var start = new DateOnly(year, month, 1);
            var end   = start.AddMonths(1).AddDays(-1);
            list.Add(new AssessmentPeriod
            {
                Name       = $"{start:MMMM} {year}",
                Frequency  = AssessmentPeriodFrequency.Monthly,
                StartDate  = start,
                EndDate    = end,
                Year       = year,
                WeekNumber = null,
                Status     = AssessmentPeriodStatus.Draft,
                IsActive   = true
            });
        }
        return list;
    }

    private static List<AssessmentPeriod> BuildQuarterlyPeriods(int year)
    {
        var list = new List<AssessmentPeriod>();
        int[] startMonths = { 1, 4, 7, 10 };
        string[] labels   = { "Q1", "Q2", "Q3", "Q4" };
        for (int i = 0; i < 4; i++)
        {
            var start = new DateOnly(year, startMonths[i], 1);
            var end   = start.AddMonths(3).AddDays(-1);
            list.Add(new AssessmentPeriod
            {
                Name       = $"{labels[i]} {year}",
                Frequency  = AssessmentPeriodFrequency.Quarterly,
                StartDate  = start,
                EndDate    = end,
                Year       = year,
                WeekNumber = null,
                Status     = AssessmentPeriodStatus.Draft,
                IsActive   = true
            });
        }
        return list;
    }

    private static List<AssessmentPeriod> BuildHalfYearlyPeriods(int year)
    {
        return new List<AssessmentPeriod>
        {
            new()
            {
                Name       = $"H1 {year}",
                Frequency  = AssessmentPeriodFrequency.HalfYearly,
                StartDate  = new DateOnly(year, 1, 1),
                EndDate    = new DateOnly(year, 6, 30),
                Year       = year,
                WeekNumber = null,
                Status     = AssessmentPeriodStatus.Draft,
                IsActive   = true
            },
            new()
            {
                Name       = $"H2 {year}",
                Frequency  = AssessmentPeriodFrequency.HalfYearly,
                StartDate  = new DateOnly(year, 7, 1),
                EndDate    = new DateOnly(year, 12, 31),
                Year       = year,
                WeekNumber = null,
                Status     = AssessmentPeriodStatus.Draft,
                IsActive   = true
            }
        };
    }

    private static List<AssessmentPeriod> BuildAnnualPeriods(int year)
    {
        return new List<AssessmentPeriod>
        {
            new()
            {
                Name       = $"Annual {year}",
                Frequency  = AssessmentPeriodFrequency.Annual,
                StartDate  = new DateOnly(year, 1, 1),
                EndDate    = new DateOnly(year, 12, 31),
                Year       = year,
                WeekNumber = null,
                Status     = AssessmentPeriodStatus.Draft,
                IsActive   = true
            }
        };
    }
}
