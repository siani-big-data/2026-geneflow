namespace GeneFlow.ApiNet2.Application.Search.Common;

/// <summary>
/// Cursor-paginated result envelope.
/// </summary>
public sealed record CursorPagedList<T>(IReadOnlyList<T> Items, string? NextCursor)
{
    public static CursorPagedList<T> Empty() => new(Array.Empty<T>(), null);
}
