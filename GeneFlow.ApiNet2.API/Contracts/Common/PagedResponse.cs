using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.API.Contracts.Common;

/// <summary>
/// Paged response wrapper.
/// </summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

/// <summary>
/// Extension methods for paged responses.
/// </summary>
public static class PagedResponseExtensions
{
    /// <summary>
    /// Converts a PagedList to a PagedResponse.
    /// </summary>
    public static PagedResponse<TResult> ToPagedResponse<T, TResult>(
        this PagedList<T> pagedList,
        Func<T, TResult> mapper) => new(
        pagedList.Items.Select(mapper).ToList(),
        pagedList.PageNumber,
        pagedList.PageSize,
        pagedList.TotalCount,
        pagedList.TotalPages,
        pagedList.HasPreviousPage,
        pagedList.HasNextPage);
}
