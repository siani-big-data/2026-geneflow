using System.Text.Json;

namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// Annotation data transfer object.
/// </summary>
public sealed record AnnotationDto
{
    public required string Id { get; init; }
    public required string TraceId { get; init; }
    public required string Type { get; init; }
    public required int TypeId { get; init; }
    public required string Label { get; init; }
    public string? Description { get; init; }
    public required int StartPosition { get; init; }
    public required int EndPosition { get; init; }
    public required string Strand { get; init; }
    public required int StrandId { get; init; }
    public required string Color { get; init; }
    public required bool IsShared { get; init; }
    public JsonDocument? Metadata { get; init; }

    // Computed
    public int Length => EndPosition - StartPosition + 1;

    // Audit
    public required DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public string? ModifiedBy { get; init; }
}
