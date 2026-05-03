namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// Manifest containing metadata about stored trace chunks.
/// </summary>
public sealed record TraceManifestDto(
    string TraceId,
    string OriginalFilename,
    string Format,
    int TotalBases,
    int ChunkSize,
    int ChunkCount,
    bool HasChromatogram,
    bool HasQualityScores,
    string CreatedAt,
    IReadOnlyList<ChunkMetadataDto> Chunks);

/// <summary>
/// Metadata for a single chunk.
/// </summary>
public sealed record ChunkMetadataDto(
    int Index,
    int StartPosition,
    int EndPosition,
    int BaseCount,
    string Filename);

/// <summary>
/// A chunk of trace sequence data.
/// </summary>
public sealed record TraceChunkDto(
    int Index,
    int StartPosition,
    int EndPosition,
    string Bases,
    IReadOnlyList<int>? QualityScores,
    ChromatogramChunkDto? Chromatogram);

/// <summary>
/// Chromatogram data for a chunk.
/// </summary>
public sealed record ChromatogramChunkDto(
    IReadOnlyList<int>? A,
    IReadOnlyList<int>? C,
    IReadOnlyList<int>? G,
    IReadOnlyList<int>? T,
    IReadOnlyList<int>? PeakPositions);

/// <summary>
/// Paginated response for sequence data.
/// </summary>
public sealed record SequencePageDto(
    string TraceId,
    int Page,
    int PageSize,
    int TotalBases,
    int TotalPages,
    string Bases,
    IReadOnlyList<int>? QualityScores,
    ChromatogramChunkDto? Chromatogram);

/// <summary>
/// Summary of available analysis results for a trace.
/// </summary>
public sealed record AnalysisResultsSummaryDto(
    string TraceId,
    IReadOnlyList<string> AvailableAnalyses);
