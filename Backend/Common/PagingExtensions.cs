using Gridify;
using Gridify.EntityFramework;
using Spot4Hire.Backend.Dtos.Common;

namespace Spot4Hire.Backend.Common;

public static class PagingExtensions
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    // Filters, orders, and pages an entity query, then projects each row to a DTO.
    public static async Task<PagedResponse<TDto>> ToPagedResponseAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        GridifyQuery gridifyQuery,
        IGridifyMapper<TEntity> mapper,
        Func<TEntity, TDto> map,
        string defaultOrderBy,
        CancellationToken ct)
    {
        Normalize(gridifyQuery, defaultOrderBy);

        var paged = await query.GridifyAsync(gridifyQuery, ct, mapper);

        return new PagedResponse<TDto>
        {
            Items = paged.Data.Select(map).ToList(),
            Page = gridifyQuery.Page,
            PageSize = gridifyQuery.PageSize,
            TotalCount = paged.Count,
        };
    }

    // Clamp paging and default the sort so paging is stable when the caller gives no orderBy.
    public static void Normalize(GridifyQuery gridifyQuery, string defaultOrderBy)
    {
        gridifyQuery.Page = gridifyQuery.Page < 1 ? 1 : gridifyQuery.Page;
        gridifyQuery.PageSize = gridifyQuery.PageSize < 1
            ? DefaultPageSize
            : Math.Min(gridifyQuery.PageSize, MaxPageSize);

        if (string.IsNullOrWhiteSpace(gridifyQuery.OrderBy))
        {
            gridifyQuery.OrderBy = defaultOrderBy;
        }
    }
}
