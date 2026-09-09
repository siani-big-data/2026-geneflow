using System.Text.Json;

namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Annotation response.
/// </summary>
public sealed class AnnotationResponse
{
    public string Id { get; init; } = null!;
    public string TraceId { get; init; } = null!;
    public string Type { get; init; } = null!;
    public int TypeId { get; init; }
    public string Label { get; init; } = null!;
    public string? Description { get; init; }
    public int StartPosition { get; init; }
    public int EndPosition { get; init; }
    public string Strand { get; init; } = null!;
    public int StrandId { get; init; }
    public string Color { get; init; } = null!;
    public bool IsShared { get; init; }
    public JsonDocument? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public string? ModifiedBy { get; init; }
}
