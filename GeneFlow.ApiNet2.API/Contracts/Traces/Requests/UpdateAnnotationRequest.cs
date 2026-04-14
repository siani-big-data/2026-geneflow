using System.Text.Json;

namespace GeneFlow.ApiNet2.API.Contracts.Traces.Requests;

/// <summary>
/// Request to update an annotation on a trace.
/// </summary>
public sealed record UpdateAnnotationRequest(
    string Label,
    string? Description,
    int StartPosition,
    int EndPosition,
    int StrandId,
    string Color,
    bool IsShared = false,
    JsonDocument? Metadata = null);
