using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UndoAllSequenceEdits;

/// <summary>
/// Command to undo all active sequence edits on a trace.
/// </summary>
public sealed record UndoAllSequenceEditsCommand(
    string UserId,
    string TraceId) : ICommand<Result>;
