using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UndoSequenceEdit;

/// <summary>
/// Command to undo a specific sequence edit on a trace.
/// </summary>
public sealed record UndoSequenceEditCommand(
    string UserId,
    string TraceId,
    string EditId) : ICommand<Result>;
