namespace GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

/// <summary>
/// Represents a paginated list of items.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public sealed class PagedList<T>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// The total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// The index of the first item on this page (1-based).
    /// </summary>
    public int FirstItemIndex => TotalCount == 0 ? 0 : (PageNumber - 1) * PageSize + 1;

    /// <summary>
    /// The index of the last item on this page (1-based).
    /// </summary>
    public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalCount);

    private PagedList(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Creates a paged list from items and pagination info.
    /// </summary>
    public static PagedList<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
        => new(items, pageNumber, pageSize, totalCount);

    /// <summary>
    /// Creates a paged list from a queryable source.
    /// </summary>
    public static PagedList<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        var items = source.ToList();
        var totalCount = items.Count;
        var pagedItems = items
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedList<T>(pagedItems, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Creates an empty paged list.
    /// </summary>
    public static PagedList<T> Empty(int pageSize = PagedRequest.DefaultPageSize)
        => new([], 1, pageSize, 0);

    /// <summary>
    /// Maps the items to a different type.
    /// </summary>
    public PagedList<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        var mappedItems = Items.Select(mapper).ToList();
        return PagedList<TResult>.Create(mappedItems, PageNumber, PageSize, TotalCount);
    }

    /// <summary>
    /// Maps the items to a different type asynchronously.
    /// </summary>
    public async Task<PagedList<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> mapper)
    {
        var mappedItems = new List<TResult>(Items.Count);
        foreach (var item in Items)
        {
            mappedItems.Add(await mapper(item));
        }
        return PagedList<TResult>.Create(mappedItems, PageNumber, PageSize, TotalCount);
    }
}
