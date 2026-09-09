using GeneFlow.ApiNet2.Application.Traces.DTOs;

namespace GeneFlow.ApiNet2.Application.Traces.Interfaces;

/// <summary>
/// Client for reading trace data from the Datalake storage (MinIO/S3).
/// </summary>
public interface IDatalakeStorageClient
{
    /// <summary>
    /// Gets the manifest for a trace containing chunk metadata.
    /// </summary>
    /// <param name="traceId">The trace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The manifest if found, null otherwise.</returns>
    Task<TraceManifestDto?> GetManifestAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific chunk of trace sequence data.
    /// </summary>
    /// <param name="traceId">The trace ID.</param>
    /// <param name="chunkIndex">Zero-based chunk index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chunk if found, null otherwise.</returns>
    Task<TraceChunkDto?> GetChunkAsync(string traceId, int chunkIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the original trace file (AB1, SCF, etc.).
    /// </summary>
    /// <param name="traceId">The trace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of file data and extension if found, null otherwise.</returns>
    Task<(byte[] Data, string Extension)?> GetOriginalFileAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a stored analysis result for a trace.
    /// </summary>
    /// <typeparam name="T">The result type to deserialize to.</typeparam>
    /// <param name="traceId">The trace ID.</param>
    /// <param name="analysisType">Analysis type (trimming, heterozygote, motif, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result if found, null otherwise.</returns>
    Task<T?> GetAnalysisResultAsync<T>(string traceId, string analysisType, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Gets a stored analysis result as raw JSON.
    /// </summary>
    /// <param name="traceId">The trace ID.</param>
    /// <param name="analysisType">Analysis type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JSON string if found, null otherwise.</returns>
    Task<string?> GetAnalysisResultJsonAsync(string traceId, string analysisType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists available analysis results for a trace.
    /// </summary>
    /// <param name="traceId">The trace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of analysis type names.</returns>
    Task<IReadOnlyList<string>> ListAnalysisResultsAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if chunked data exists for a trace.
    /// </summary>
    /// <param name="traceId">The trace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if manifest exists.</returns>
    Task<bool> HasChunkedDataAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the storage is healthy and accessible.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if healthy.</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
