using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UndoTrimTrace;

/// <summary>
/// Command to undo a specific trim or all trims from a trace.
/// If TrimId is provided, only that trim is undone.
/// If UndoAll is true, all active trims are undone.
/// Requires Editor role or higher in the trace's parent study.
/// </summary>
public sealed record UndoTrimTraceCommand(
    string UserId,
    string TraceId,
    string? TrimId = null,
    bool UndoAll = false) : ICommand<Result>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
    StudyRole? IRequireTraceAccess.MinimumRole => StudyRole.Editor;
}
