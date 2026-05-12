namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure;

/// <summary>
/// Metadata describing a stored analysis result.
/// </summary>
/// <param name="AnalysisType">Canonical analysis type identifier (e.g. "trimming", "orf", "motif").</param>
/// <param name="StoredAt">UTC timestamp when the result was persisted.</param>
public sealed record AnalysisResultMetadata(string AnalysisType, DateTime StoredAt);

/// <summary>
/// Store for analysis results produced by the analysis worker.
/// Acts as a read-through cache so the API can serve smart-tool results
/// (trimming, ORF, motif, translation, restriction, heterozygote, quality)
/// without round-tripping to the datalake on every request.
/// </summary>
public interface IAnalysisResultStore
{
    /// <summary>
    /// Persists the JSON payload for a given trace and analysis type.
    /// Replaces any prior payload for that pair and updates the per-trace index.
    /// </summary>
    /// <param name="traceId">The trace identifier.</param>
    /// <param name="analysisType">Canonical analysis type (case-insensitive; will be normalised to lower-case).</param>
    /// <param name="payloadJson">Serialised JSON payload for the result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(
        string traceId,
        string analysisType,
        string payloadJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the JSON payload for a given trace and analysis type, or null if no result is stored.
    /// </summary>
    /// <param name="traceId">The trace identifier.</param>
    /// <param name="analysisType">Canonical analysis type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JSON payload as stored, or null when not found.</returns>
    Task<string?> GetAsync(
        string traceId,
        string analysisType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists every analysis result currently stored for a trace.
    /// </summary>
    /// <param name="traceId">The trace identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Metadata for each stored result; empty list when none exist.</returns>
    Task<IReadOnlyList<AnalysisResultMetadata>> ListAsync(
        string traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every stored analysis result for a trace.
    /// Intended for trace deletion / re-processing flows.
    /// </summary>
    /// <param name="traceId">The trace identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAllAsync(
        string traceId,
        CancellationToken cancellationToken = default);
}
