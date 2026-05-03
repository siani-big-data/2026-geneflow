namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure;

/// <summary>
/// Interface for publishing jobs to the analysis worker via Redis Streams.
/// </summary>
public interface IJobPublisher
{
    /// <summary>
    /// Publishes a trace processing job to the trace worker queue.
    /// </summary>
    /// <param name="job">The trace processing job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishTraceJobAsync(TraceProcessingJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes an alignment job to the alignment worker queue.
    /// </summary>
    /// <param name="job">The alignment job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAlignmentJobAsync(AlignmentJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes an analysis job to the analysis worker queue.
    /// </summary>
    /// <param name="job">The analysis job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAnalysisJobAsync(AnalysisJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a pipeline job to the pipeline worker queue.
    /// </summary>
    /// <param name="job">The pipeline job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishPipelineJobAsync(PipelineJob job, CancellationToken cancellationToken = default);
}

/// <summary>
/// Job for processing a trace file (parsing, quality analysis).
/// </summary>
/// <param name="TraceId">The trace identifier.</param>
/// <param name="StudyId">The study identifier.</param>
/// <param name="FileName">The original file name.</param>
/// <param name="StoragePath">The storage path where the file is located.</param>
/// <param name="Format">The trace format (ABI, SCF, FASTQ, FASTA).</param>
/// <param name="Options">Optional processing options.</param>
public sealed record TraceProcessingJob(
    string TraceId,
    string StudyId,
    string FileName,
    string StoragePath,
    string Format,
    Dictionary<string, object>? Options = null);

/// <summary>
/// Job for sequence alignment.
/// </summary>
/// <param name="AlignmentId">The alignment identifier.</param>
/// <param name="Type">The alignment type (pairwise, multiple).</param>
/// <param name="TraceIds">The trace identifiers to align.</param>
/// <param name="Sequences">Optional pre-extracted sequences (if not using traces).</param>
/// <param name="Options">Alignment options.</param>
public sealed record AlignmentJob(
    string AlignmentId,
    string Type,
    List<string> TraceIds,
    List<string>? Sequences = null,
    AlignmentJobOptions? Options = null);

/// <summary>
/// Options for alignment jobs.
/// </summary>
/// <param name="BuildConsensus">Whether to build a consensus sequence.</param>
/// <param name="ConsensusMethod">The consensus method (majority, threshold).</param>
/// <param name="MatchScore">Score for matching bases.</param>
/// <param name="MismatchPenalty">Penalty for mismatches.</param>
/// <param name="GapPenalty">Penalty for gaps.</param>
public sealed record AlignmentJobOptions(
    bool BuildConsensus = true,
    string ConsensusMethod = "majority",
    int MatchScore = 1,
    int MismatchPenalty = -1,
    int GapPenalty = -2);

/// <summary>
/// Job for running a specific analysis on a trace.
/// </summary>
/// <param name="TraceId">The trace identifier.</param>
/// <param name="AnalysisType">The type of analysis (trimming, heterozygote, motif, translation, orf, restriction).</param>
/// <param name="Sequence">The sequence to analyze (optional, fetched from trace if not provided).</param>
/// <param name="Quality">Quality scores (optional).</param>
/// <param name="Options">Analysis-specific options.</param>
public sealed record AnalysisJob(
    string TraceId,
    string AnalysisType,
    string? Sequence = null,
    int[]? Quality = null,
    Dictionary<string, object>? Options = null);

/// <summary>
/// Job for executing a pipeline on a trace.
/// </summary>
/// <param name="ExecutionId">The pipeline execution identifier.</param>
/// <param name="PipelineId">The pipeline identifier.</param>
/// <param name="TraceId">The trace identifier.</param>
/// <param name="Steps">The steps to execute in order.</param>
public sealed record PipelineJob(
    string ExecutionId,
    string PipelineId,
    string TraceId,
    List<PipelineStepJob> Steps);

/// <summary>
/// Individual step within a pipeline job.
/// </summary>
/// <param name="StepExecutionId">The step execution identifier.</param>
/// <param name="Order">The execution order.</param>
/// <param name="StepType">The analysis type to run.</param>
/// <param name="Configuration">The step configuration.</param>
public sealed record PipelineStepJob(
    Guid StepExecutionId,
    int Order,
    string StepType,
    Dictionary<string, object> Configuration);
