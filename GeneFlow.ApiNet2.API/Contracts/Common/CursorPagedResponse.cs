using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.API.Contracts.Common;

/// <summary>
/// Wire representation of a cursor (keyset) paginated response.
/// </summary>
/// <typeparam name="T">Type of item carried in the page.</typeparam>
/// <param name="Items">Items in the current page.</param>
/// <param name="NextCursor">
/// Opaque cursor that can be passed back to fetch the next page, or <c>null</c>
/// when the caller has reached the tail of the feed.
/// </param>
/// <param name="HasMore">
/// True when more items may exist beyond this page. Clients can keep fetching
/// while this flag is true.
/// </param>
public sealed record CursorPagedResponse<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore);

/// <summary>
/// Extension methods to project <see cref="CursorPaged{T}"/> values into
/// <see cref="CursorPagedResponse{T}"/> contracts.
/// </summary>
public static class CursorPagedResponseExtensions
{
    /// <summary>Maps a <see cref="CursorPaged{T}"/> page into the API contract.</summary>
    public static CursorPagedResponse<TResult> ToCursorPagedResponse<T, TResult>(
        this CursorPaged<T> page,
        Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(mapper);
        return new CursorPagedResponse<TResult>(
            page.Items.Select(mapper).ToList(),
            page.NextCursor,
            page.HasMore);
    }
}
