namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Response containing trace manifest metadata from datalake storage.
/// </summary>
public sealed class TraceManifestResponse
{
    public string TraceId { get; init; } = null!;
    public string OriginalFilename { get; init; } = null!;
    public string Format { get; init; } = null!;
    public int TotalBases { get; init; }
    public int ChunkSize { get; init; }
    public int ChunkCount { get; init; }
    public bool HasChromatogram { get; init; }
    public bool HasQualityScores { get; init; }
    public string CreatedAt { get; init; } = null!;
    public IReadOnlyList<ChunkMetadataResponse> Chunks { get; init; } = Array.Empty<ChunkMetadataResponse>();
}

/// <summary>
/// Metadata for a single chunk in the trace manifest.
/// </summary>
public sealed class ChunkMetadataResponse
{
    public int Index { get; init; }
    public int StartPosition { get; init; }
    public int EndPosition { get; init; }
    public int BaseCount { get; init; }
}
