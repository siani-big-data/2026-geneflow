using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Orchestration;

/// <summary>
/// Maps a pipeline step (its <see cref="StepType.AnalysisKey"/>) to an analysis
/// job published on the existing <c>geneflow:jobs:analysis</c> stream. Quality
/// steps are reported as instant completions because the metric is already
/// computed at trace-processing time (see plan / out-of-scope notes).
/// </summary>
internal static class PipelineStepDispatcher
{
    /// <summary>
    /// Quality is computed when the trace itself is processed; treat it as an
    /// instant success without round-tripping through the analysis worker.
    /// </summary>
    public const string QualityKey = "quality";

    /// <summary>
    /// Returns true when the step does not need to be dispatched to the
    /// analysis worker (e.g. quality, which is computed up-front).
    /// </summary>
    public static bool IsInstant(string analysisKey) =>
        string.Equals(analysisKey, QualityKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Step types whose Python analyser requires Phred quality scores. Trimming
    /// and heterozygote detection both branch on per-base quality. The Python
    /// worker raises <c>ValueError("Quality scores required for ...")</c> and
    /// (currently) does not publish a <c>*Failed</c> event, so the orchestrator
    /// would otherwise hang until <c>StepTimeout</c>. We refuse to publish in
    /// that case and fail the step immediately with a clear message.
    /// </summary>
    private static readonly HashSet<string> QualityRequiredKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "trimming",
        "heterozygote"
    };

    /// <summary>
    /// True when the step's analyser requires non-empty Phred quality scores.
    /// </summary>
    public static bool RequiresQuality(string analysisKey) =>
        QualityRequiredKeys.Contains(analysisKey);

    /// <summary>
    /// Publishes the analysis job for a non-instant step.
    /// </summary>
    public static Task PublishAsync(
        IJobPublisher publisher,
        string traceId,
        string analysisKey,
        string sequence,
        int[] quality,
        Dictionary<string, object> configuration,
        CancellationToken cancellationToken)
    {
        var job = new AnalysisJob(
            TraceId: traceId,
            AnalysisType: analysisKey,
            Sequence: sequence,
            Quality: quality,
            Options: configuration);

        return publisher.PublishAnalysisJobAsync(job, cancellationToken);
    }
}
