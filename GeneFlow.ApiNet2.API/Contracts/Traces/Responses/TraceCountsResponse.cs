namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Trace counts by status response.
/// </summary>
public sealed class TraceCountsResponse
{
    public int Total { get; init; }
    public int Uploaded { get; init; }
    public int Validating { get; init; }
    public int Processing { get; init; }
    public int Processed { get; init; }
    public int Failed { get; init; }
    public int Archived { get; init; }
}
