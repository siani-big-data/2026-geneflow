namespace GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

/// <summary>
/// Extension methods for creating paged lists.
/// </summary>
public static class PagedListExtensions
{
    /// <summary>
    /// Creates a paged list from a queryable.
    /// </summary>
    public static PagedList<T> ToPagedList<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize)
    {
        var totalCount = source.Count();
        var items = source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return PagedList<T>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Creates a paged list from a queryable using a PagedRequest.
    /// </summary>
    public static PagedList<T> ToPagedList<T>(
        this IQueryable<T> source,
        PagedRequest request)
    {
        return source.ToPagedList(request.PageNumber, request.PageSize);
    }

    /// <summary>
    /// Creates a paged list from an enumerable.
    /// </summary>
    public static PagedList<T> ToPagedList<T>(
        this IEnumerable<T> source,
        int pageNumber,
        int pageSize)
    {
        var items = source.ToList();
        var totalCount = items.Count;
        var pagedItems = items
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return PagedList<T>.Create(pagedItems, pageNumber, pageSize, totalCount);
    }
}
