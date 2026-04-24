using Microsoft.EntityFrameworkCore;

namespace KnowHub.Infrastructure.Persistence.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int pageNumber, int pageSize)
    {
        var sanitisedPageNumber = Math.Max(1, pageNumber);
        var sanitisedPageSize = Math.Clamp(pageSize, 1, 100);
        return query.Skip((sanitisedPageNumber - 1) * sanitisedPageSize).Take(sanitisedPageSize);
    }

    public static async Task<(List<T> Data, int TotalCount)> ToPagedListAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query.ApplyPaging(pageNumber, pageSize).ToListAsync(cancellationToken);
        return (data, totalCount);
    }
}
