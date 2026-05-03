using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.ManualTrimTrace;

/// <summary>
/// Command to add a manual trim to a trace sequence.
/// Multiple trims can be added to a single trace.
/// Requires Editor role or higher in the trace's parent study.
/// </summary>
public sealed record ManualTrimTraceCommand(
    string UserId,
    string TraceId,
    int StartPosition,
    int EndPosition,
    string TrimEnd,
    string? Reason = null) : ICommand<Result<TraceTrimDto>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
    StudyRole? IRequireTraceAccess.MinimumRole => StudyRole.Editor;
}
