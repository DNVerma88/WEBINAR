using KnowHub.Application.Contracts;
using KnowHub.Application.Contracts.Analytics;
using KnowHub.Domain.Enums;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KnowHub.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly KnowHubDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IMemoryCache _cache;

    public AnalyticsService(KnowHubDbContext db, ICurrentUserAccessor currentUser, IMemoryCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<AnalyticsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var cacheKey = $"analytics:summary:{tenantId}";

        // API-08: cache the summary for 5 minutes — it changes infrequently
        if (_cache.TryGetValue(cacheKey, out AnalyticsSummaryResponse? cached) && cached is not null)
            return cached;

        var weekAgo = DateTime.UtcNow.AddDays(-7);

        var totalSessions = await _db.Sessions.CountAsync(s => s.TenantId == tenantId, cancellationToken);
        var totalAssets = await _db.KnowledgeAssets.CountAsync(a => a.TenantId == tenantId, cancellationToken);
        var totalUsers = await _db.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive, cancellationToken);

        // API-03: compute pass-rate in SQL — avoid loading all IsPassed booleans into memory
        var totalAttempts = await _db.UserQuizAttempts.CountAsync(a => a.TenantId == tenantId, cancellationToken);
        var passedAttempts = await _db.UserQuizAttempts.CountAsync(a => a.TenantId == tenantId && a.IsPassed == true, cancellationToken);
        var avgPassRate = totalAttempts > 0 ? passedAttempts / (double)totalAttempts * 100 : 0;

        var weeklyActiveUsers = await _db.UserXpEvents
            .Where(e => e.TenantId == tenantId && e.CreatedDate >= weekAgo)
            .Select(e => e.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var categoryList = await _db.Categories
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .AsNoTracking()
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);

        var sessionCountsByCategory = await _db.Sessions
            .Where(s => s.TenantId == tenantId)
            .GroupBy(s => s.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var assetCountsByCategory = await _db.KnowledgeAssets
            .Where(a => a.TenantId == tenantId && a.SessionId.HasValue)
            .Join(_db.Sessions, a => a.SessionId!.Value, s => s.Id, (a, s) => s.CategoryId)
            .GroupBy(categoryId => categoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // API-21: use Dictionary lookups instead of O(N²) in-loop FirstOrDefault on List
        var sessionDict = sessionCountsByCategory.ToDictionary(s => s.CategoryId, s => s.Count);
        var assetDict = assetCountsByCategory.ToDictionary(a => a.CategoryId, a => a.Count);

        var topCategories = categoryList
            .Select(c => new TopCategoryItem(
                c.Id,
                c.Name,
                sessionDict.GetValueOrDefault(c.Id, 0),
                assetDict.GetValueOrDefault(c.Id, 0)
            ))
            .OrderByDescending(c => c.SessionCount)
            .Take(5)
            .ToList();

        var result = new AnalyticsSummaryResponse(
            totalSessions, totalAssets, totalUsers,
            Math.Round(avgPassRate, 1), weeklyActiveUsers, topCategories);

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<KnowledgeGapHeatmapResponse> GetKnowledgeGapHeatmapAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var categories = await _db.Categories
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var departments = await _db.Users
            .Where(u => u.TenantId == tenantId && u.IsActive && u.Department != null)
            .Select(u => u.Department!)
            .Distinct()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var quizDataByCategory = await _db.UserQuizAttempts
            .Where(a => a.TenantId == tenantId)
            .Join(_db.SessionQuizzes, a => a.QuizId, q => q.Id, (a, q) => new { a.IsPassed, q.SessionId })
            .Join(_db.Sessions, x => x.SessionId, s => s.Id, (x, s) => new { x.IsPassed, s.CategoryId })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var sessionCountByCategory = await _db.Sessions
            .Where(s => s.TenantId == tenantId)
            .GroupBy(s => s.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var sessionCountDict = sessionCountByCategory.ToDictionary(s => s.CategoryId, s => s.Count);

        // API-04: push registration grouping to SQL instead of loading all rows for O(C×D×N) in-memory iteration
        var registrationAgg = await _db.SessionRegistrations
            .Where(r => r.TenantId == tenantId)
            .Join(_db.Sessions, r => r.SessionId, s => s.Id, (r, s) => new { r.ParticipantId, s.CategoryId })
            .Join(_db.Users, x => x.ParticipantId, u => u.Id, (x, u) => new { x.CategoryId, u.Department })
            .Where(x => x.Department != null)
            .GroupBy(x => new { x.CategoryId, x.Department })
            .Select(g => new { g.Key.CategoryId, g.Key.Department, Count = g.Count() })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var registrationDict = registrationAgg
            .ToDictionary(r => (r.CategoryId, r.Department!), r => r.Count);

        var quizByCategoryDict = quizDataByCategory
            .GroupBy(q => q.CategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var cells = new List<HeatmapCell>();

        foreach (var category in categories)
        {
            var catSessionCount = sessionCountDict.GetValueOrDefault(category.Id, 0);
            var catQuizData = quizByCategoryDict.GetValueOrDefault(category.Id);

            var quizPassRate = catQuizData is { Count: > 0 }
                ? catQuizData.Count(q => q.IsPassed == true) / (double)catQuizData.Count * 100
                : 0;

            foreach (var dept in departments)
            {
                var deptRegistrations = registrationDict.GetValueOrDefault((category.Id, dept), 0);
                var engagementScore = ComputeEngagementScore(deptRegistrations, catSessionCount, quizPassRate);

                cells.Add(new HeatmapCell(
                    category.Id, category.Name, dept,
                    Math.Round(engagementScore, 1),
                    catSessionCount, 0,
                    Math.Round(quizPassRate, 1)));
            }
        }

        return new KnowledgeGapHeatmapResponse(
            cells,
            departments,
            categories.Select(c => c.Name).ToList());
    }

    private static double ComputeEngagementScore(int registrations, int sessionCount, double quizPassRate)
    {
        if (sessionCount == 0) return 0;
        var regScore = Math.Min(registrations / 10.0, 1.0) * 40;
        var quizScore = quizPassRate / 100.0 * 60;
        return regScore + quizScore;
    }

    public async Task<SkillCoverageReportResponse> GetSkillCoverageAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var categories = await _db.Categories
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var allTags = await _db.Tags
            .Where(t => t.TenantId == tenantId && t.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var usedTagIds = await _db.SessionTags
            .Join(_db.Sessions, st => st.SessionId, s => s.Id,
                (st, s) => new { st.TagId, s.CategoryId, s.TenantId })
            .Where(x => x.TenantId == tenantId)
            .Select(x => new { x.TagId, x.CategoryId })
            .Distinct()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var coverageItems = categories.Select(c =>
        {
            var coveredTagIds = usedTagIds
                .Where(u => u.CategoryId == c.Id)
                .Select(u => u.TagId)
                .Distinct()
                .ToList();

            var totalTagCount = allTags.Count;
            var coveredCount = coveredTagIds.Count;
            var coveragePercent = totalTagCount > 0 ? coveredCount / (double)totalTagCount * 100 : 0;

            var topCovered = allTags
                .Where(t => coveredTagIds.Contains(t.Id))
                .Take(5)
                .Select(t => t.Name)
                .ToList();

            var gapTags = allTags
                .Where(t => !coveredTagIds.Contains(t.Id))
                .Take(5)
                .Select(t => t.Name)
                .ToList();

            return new CategorySkillCoverage(
                c.Id, c.Name, totalTagCount, coveredCount,
                Math.Round(coveragePercent, 1), topCovered, gapTags);
        }).ToList();

        var overallCoverage = coverageItems.Count > 0
            ? coverageItems.Average(c => c.CoveragePercent)
            : 0;

        return new SkillCoverageReportResponse(coverageItems, Math.Round(overallCoverage, 1));
    }

    public async Task<ContentFreshnessReportResponse> GetContentFreshnessAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var now = DateTime.UtcNow;

        // API-07: compute age buckets in SQL with EF.Functions.DateDiffDay
        // instead of loading all rows to compute (now - CreatedDate).TotalDays in memory
        var sessionBuckets = await _db.Sessions
            .Where(s => s.TenantId == tenantId)
            .AsNoTracking()
            .Select(s => new
            {
                s.Id, s.Title, s.CreatedDate,
                AgeDays = EF.Functions.DateDiffDay(s.CreatedDate, now)
            })
            .ToListAsync(cancellationToken);

        var assetBuckets = await _db.KnowledgeAssets
            .Where(a => a.TenantId == tenantId)
            .AsNoTracking()
            .Select(a => new
            {
                a.Id, a.Title, a.CreatedDate,
                AgeDays = EF.Functions.DateDiffDay(a.CreatedDate, now)
            })
            .ToListAsync(cancellationToken);

        var sessions = sessionBuckets
            .Select(s => new ContentFreshnessItem(s.Id, "Session", s.Title, s.CreatedDate, s.AgeDays))
            .ToList();
        var assets = assetBuckets
            .Select(a => new ContentFreshnessItem(a.Id, "KnowledgeAsset", a.Title, a.CreatedDate, a.AgeDays))
            .ToList();

        var all = sessions.Concat(assets).ToList();

        return new ContentFreshnessReportResponse(
            all.Count(x => x.AgeDays < 30),
            all.Count(x => x.AgeDays >= 30 && x.AgeDays < 90),
            all.Count(x => x.AgeDays >= 90 && x.AgeDays < 180),
            all.Count(x => x.AgeDays >= 180),
            all.OrderByDescending(x => x.AgeDays).Take(10).ToList());
    }

    public async Task<LearningFunnelResponse> GetLearningFunnelAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var discovered = await _db.Sessions.CountAsync(s => s.TenantId == tenantId, cancellationToken);
        var registered = await _db.SessionRegistrations.CountAsync(r => r.TenantId == tenantId, cancellationToken);
        var attended = await _db.SessionRegistrations
            .CountAsync(r => r.TenantId == tenantId && r.Status == RegistrationStatus.Attended, cancellationToken);
        var rated = await _db.SessionRatings.CountAsync(r => r.TenantId == tenantId, cancellationToken);
        var quizPassed = await _db.UserQuizAttempts
            .CountAsync(a => a.TenantId == tenantId && a.IsPassed == true, cancellationToken);

        return new LearningFunnelResponse(
            discovered, registered, attended, rated, quizPassed,
            discovered > 0 ? Math.Round(registered / (double)discovered * 100, 1) : 0,
            registered > 0 ? Math.Round(attended / (double)registered * 100, 1) : 0,
            attended > 0 ? Math.Round(rated / (double)attended * 100, 1) : 0,
            attended > 0 ? Math.Round(quizPassed / (double)attended * 100, 1) : 0);
    }

    public async Task<CohortCompletionRatesResponse> GetCohortCompletionRatesAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // API-05: load paths and enrollments separately then join in-memory
        // to avoid the N+1 correlated subquery inside LINQ .Select()
        var paths = await _db.LearningPaths
            .Where(lp => lp.TenantId == tenantId && lp.IsPublished)
            .AsNoTracking()
            .Select(lp => new { lp.Id, lp.Title })
            .ToListAsync(cancellationToken);

        var pathIds = paths.Select(p => p.Id).ToList();

        var allEnrolments = await _db.UserLearningPathEnrollments
            .Where(e => pathIds.Contains(e.LearningPathId) && e.TenantId == tenantId)
            .AsNoTracking()
            .Select(e => new { e.LearningPathId, IsCompleted = e.CompletedAt.HasValue, e.CompletedAt, EnrolledAt = e.CreatedDate })
            .ToListAsync(cancellationToken);

        var enrolmentsByPath = allEnrolments
            .GroupBy(e => e.LearningPathId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = paths.Select(lp =>
        {
            var enrollments = enrolmentsByPath.GetValueOrDefault(lp.Id) ?? [];
            var total = enrollments.Count;
            var completed = enrollments.Count(e => e.IsCompleted);
            var completionRate = total > 0 ? Math.Round(completed / (double)total * 100, 1) : 0;
            var avgDays = enrollments
                .Where(e => e.IsCompleted && e.CompletedAt.HasValue)
                .Select(e => (e.CompletedAt!.Value - e.EnrolledAt).TotalDays)
                .DefaultIfEmpty(0)
                .Average();

            return new CohortCompletionItem(lp.Id, lp.Title, total, completed, completionRate,
                Math.Round(avgDays, 1));
        }).ToList();

        return new CohortCompletionRatesResponse(result);
    }

    public async Task<DepartmentEngagementScoreResponse> GetDepartmentEngagementAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // API-06: replace five whole-tenant loads + in-memory aggregation with
        // SQL GROUP BY aggregations per department
        var departmentUsers = await _db.Users
            .Where(u => u.TenantId == tenantId && u.IsActive && u.Department != null)
            .AsNoTracking()
            .Select(u => new { u.Id, u.Department })
            .ToListAsync(cancellationToken);

        var departments = departmentUsers.GroupBy(u => u.Department!).ToList();

        var sessionsByDept = await _db.SessionRegistrations
            .Where(r => r.TenantId == tenantId && r.Status == RegistrationStatus.Attended)
            .Join(_db.Users, r => r.ParticipantId, u => u.Id, (r, u) => new { u.Department })
            .Where(x => x.Department != null)
            .GroupBy(x => x.Department!)
            .Select(g => new { Department = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var assetsByDept = await _db.KnowledgeAssets
            .Where(a => a.TenantId == tenantId)
            .Join(_db.Users, a => a.CreatedBy, u => u.Id, (a, u) => new { u.Department })
            .Where(x => x.Department != null)
            .GroupBy(x => x.Department!)
            .Select(g => new { Department = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var xpByUser = await _db.UserXpEvents
            .Where(e => e.TenantId == tenantId)
            .GroupBy(e => e.UserId)
            .Select(g => new { UserId = g.Key, TotalXp = g.Sum(e => e.XpAmount) })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var quizByUser = await _db.UserQuizAttempts
            .Where(a => a.TenantId == tenantId)
            .AsNoTracking()
            .Select(a => new { a.UserId, a.IsPassed })
            .ToListAsync(cancellationToken);

        var sessionsDict = sessionsByDept.ToDictionary(x => x.Department, x => x.Count);
        var assetsDict   = assetsByDept.ToDictionary(x => x.Department, x => x.Count);

        var result = departments.Select(dept =>
        {
            var deptUserIds = dept.Select(u => u.Id).ToHashSet();
            var sessionsAttended = sessionsDict.GetValueOrDefault(dept.Key, 0);
            var assetsCreated   = assetsDict.GetValueOrDefault(dept.Key, 0);
            var xpEarned = xpByUser.Where(x => deptUserIds.Contains(x.UserId)).Sum(x => x.TotalXp);
            var deptQuizAttempts = quizByUser.Where(q => deptUserIds.Contains(q.UserId)).ToList();
            var avgPassRate = deptQuizAttempts.Count > 0
                ? deptQuizAttempts.Count(q => q.IsPassed == true) / (double)deptQuizAttempts.Count * 100
                : 0;

            var engagementScore = Math.Min(sessionsAttended / 10.0, 1.0) * 30
                + Math.Min(assetsCreated / 5.0, 1.0) * 20
                + Math.Min(xpEarned / 1000.0, 1.0) * 30
                + avgPassRate / 100.0 * 20;

            return new DepartmentEngagementItem(
                dept.Key!, sessionsAttended, assetsCreated, xpEarned,
                Math.Round(avgPassRate, 1), Math.Round(engagementScore, 1));
        }).OrderByDescending(d => d.EngagementScore).ToList();

        return new DepartmentEngagementScoreResponse(result);
    }

    public async Task<KnowledgeRetentionScoreResponse> GetKnowledgeRetentionAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var categories = await _db.Categories
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // API-22: load aggregated attempt data per category from SQL
        // instead of loading entire quiz attempts table then re-filtering per category
        var quizAttemptsByCategory = await _db.UserQuizAttempts
            .Where(a => a.TenantId == tenantId)
            .Join(_db.SessionQuizzes, a => a.QuizId, q => q.Id, (a, q) => new { a.IsPassed, a.CreatedDate, q.SessionId })
            .Join(_db.Sessions, x => x.SessionId, s => s.Id, (x, s) => new { x.IsPassed, x.CreatedDate, s.CategoryId })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var attemptsByCategoryDict = quizAttemptsByCategory
            .GroupBy(a => a.CategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = categories.Select(cat =>
        {
            var catAttempts = attemptsByCategoryDict.GetValueOrDefault(cat.Id) ?? [];

            var totalAttempts = catAttempts.Count;
            var passRate = totalAttempts > 0
                ? Math.Round(catAttempts.Count(x => x.IsPassed == true) / (double)totalAttempts * 100, 1)
                : 0;

            var sorted = catAttempts.OrderBy(a => a.CreatedDate).ToList();
            var avgDaysBetween = sorted.Count >= 2
                ? sorted.Zip(sorted.Skip(1), (a, b) => (b.CreatedDate - a.CreatedDate).TotalDays).Average()
                : 0;

            return new CategoryRetentionItem(cat.Id, cat.Name, totalAttempts, passRate,
                Math.Round(avgDaysBetween, 1), passRate);
        }).ToList();

        var overallScore = result.Count > 0 ? Math.Round(result.Average(r => r.RetentionScore), 1) : 0;

        return new KnowledgeRetentionScoreResponse(result, overallScore);
    }
}
