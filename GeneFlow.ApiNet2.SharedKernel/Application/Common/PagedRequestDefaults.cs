namespace GeneFlow.ApiNet2.SharedKernel.Application.Common;

/// <summary>
/// Default values used when paged-list endpoints are called without an
/// explicit <c>pageNumber</c> or <c>pageSize</c> query parameter, or when
/// the caller supplies non-positive values that need to be clamped.
/// </summary>
public static class PagedRequestDefaults
{
    /// <summary>
    /// Default page index (1-based) used when no page number is supplied.
    /// </summary>
    public const int PageNumber = 1;

    /// <summary>
    /// Default page size used when no page size is supplied.
    /// Chosen to balance payload size and round-trip count for typical
    /// list views in the UI.
    /// </summary>
    public const int PageSize = 20;
}
