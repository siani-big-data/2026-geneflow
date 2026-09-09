using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Search;

/// <summary>
/// Errors raised by the Search bounded context.
/// </summary>
public static class SearchErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Search.NotFound", "Search index entry was not found.");

    public static readonly Error ObjectIdRequired =
        Error.Validation("Search.ObjectIdRequired", "Object id is required.");

    public static readonly Error TitleRequired =
        Error.Validation("Search.TitleRequired", "Title is required.");

    public static readonly Error QueryRequired =
        Error.Validation("Search.QueryRequired", "Search query is required.");

    public static readonly Error InvalidCursor =
        Error.Validation("Search.InvalidCursor", "Pagination cursor is malformed.");

    public static readonly Error InvalidObjectType =
        Error.Validation("Search.InvalidObjectType", "Object type is not supported.");
}
