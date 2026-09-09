namespace GeneFlow.ApiNet2.API.Contracts.Traces.Requests;

/// <summary>
/// Request to fail trace processing (Worker API).
/// </summary>
public sealed record FailProcessingRequest(string Reason);
