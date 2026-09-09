namespace GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

/// <summary>
/// Application-layer container for cursor (keyset) paginated reads.
/// </summary>
/// <typeparam name="T">Type of item carried in the page.</typeparam>
/// <param name="Items">Items in the current page, ordered by the cursor key.</param>
/// <param name="NextCursor">
/// Opaque cursor pointing to the next page, or <c>null</c> when the consumer
/// can deduce the end of the feed (e.g. fewer items than the requested limit).
/// </param>
/// <param name="HasMore">
/// Heuristic flag: true when the page is full and another fetch may yield more
/// items. Callers may keep paging while this is true and stop on the first
/// page that returns false.
/// </param>
public sealed record CursorPaged<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore);
