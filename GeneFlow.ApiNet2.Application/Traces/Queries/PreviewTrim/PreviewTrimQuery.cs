using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.PreviewTrim;

/// <summary>
/// Query to preview auto-trim results without applying them.
/// Requires Editor role or higher in the trace's parent study.
/// </summary>
public sealed record PreviewTrimQuery(
    string TraceId,
    int QualityThreshold = 20,
    int WindowSize = 10,
    int MinimumLength = 50) : IQuery<Result<TrimPreviewDto>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
    StudyRole? IRequireTraceAccess.MinimumRole => StudyRole.Editor;
}
