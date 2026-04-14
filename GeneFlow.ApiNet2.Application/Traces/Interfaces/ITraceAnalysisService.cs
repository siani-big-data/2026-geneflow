using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Interfaces;

/// <summary>
/// Service for reading trace analysis data (sequence and quality scores) from storage.
/// </summary>
public interface ITraceAnalysisService
{
    /// <summary>
    /// Gets the quality scores array for a trace from its analysis file.
    /// </summary>
    /// <param name="trace">The trace to get quality scores for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Array of quality scores (Phred scores) for each base.</returns>
    Task<Result<int[]>> GetQualityScoresAsync(Trace trace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the sequence bases for a trace from its analysis file.
    /// </summary>
    /// <param name="trace">The trace to get sequence for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The DNA sequence as a string.</returns>
    Task<Result<string>> GetSequenceAsync(Trace trace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets both sequence and quality scores for a trace.
    /// </summary>
    /// <param name="trace">The trace to get analysis data for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple containing sequence and quality scores.</returns>
    Task<Result<(string Sequence, int[] QualityScores)>> GetAnalysisDataAsync(
        Trace trace,
        CancellationToken cancellationToken = default);
}
