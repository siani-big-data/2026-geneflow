using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.AutoTrimTrace;

/// <summary>
/// Command to auto-trim a trace using quality-based algorithm (e.g., Mott).
/// Creates two trim operations: one for 5' end and one for 3' end.
/// Requires Editor role or higher in the trace's parent study.
/// </summary>
public sealed record AutoTrimTraceCommand(
    string UserId,
    string TraceId,
    int QualityThreshold = 20,
    int WindowSize = 10,
    int MinimumLength = 50) : ICommand<Result<IReadOnlyList<TraceTrimDto>>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
    StudyRole? IRequireTraceAccess.MinimumRole => StudyRole.Editor;
}
