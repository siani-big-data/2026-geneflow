namespace GeneFlow.ApiNet2.API.Contracts.Traces.Requests;

/// <summary>
/// Request to upload a trace file.
/// </summary>
public sealed record UploadTraceRequest(
    string Name,
    string? Description);
