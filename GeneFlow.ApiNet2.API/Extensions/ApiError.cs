namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Represents an API error response.
/// </summary>
public sealed record ApiError(
    string Code,
    string Message,
    IDictionary<string, string[]>? Errors = null);
