using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UndoAllSequenceEdits;

/// <summary>
/// Command to undo all active sequence edits on a trace.
/// Requires Editor role or higher in the trace's parent study.
/// </summary>
public sealed record UndoAllSequenceEditsCommand(
    string UserId,
    string TraceId) : ICommand<Result>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
    StudyRole? IRequireTraceAccess.MinimumRole => StudyRole.Editor;
}
