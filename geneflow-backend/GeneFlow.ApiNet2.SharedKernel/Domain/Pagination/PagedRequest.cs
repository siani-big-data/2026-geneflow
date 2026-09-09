namespace GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

/// <summary>
/// Represents a request for paginated data.
/// </summary>
public record PagedRequest
{
    /// <summary>
    /// The default page size when not specified.
    /// </summary>
    public const int DefaultPageSize = 10;

    /// <summary>
    /// The maximum allowed page size.
    /// </summary>
    public const int MaxPageSize = 100;

    private int _pageNumber = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// The page number (1-based).
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
    }

    /// <summary>
    /// Optional sort field.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Sort direction (true = descending).
    /// </summary>
    public bool SortDescending { get; init; }

    /// <summary>
    /// Number of items to skip.
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Creates a default paged request.
    /// </summary>
    public static PagedRequest Default => new();

    /// <summary>
    /// Creates a paged request for a specific page.
    /// </summary>
    public static PagedRequest ForPage(int pageNumber, int pageSize = DefaultPageSize)
        => new() { PageNumber = pageNumber, PageSize = pageSize };
}

/// <summary>
/// Represents a request for paginated data with search/filter.
/// </summary>
public record PagedRequest<TFilter> : PagedRequest where TFilter : class, new()
{
    /// <summary>
    /// Filter criteria.
    /// </summary>
    public TFilter Filter { get; init; } = new();
}
