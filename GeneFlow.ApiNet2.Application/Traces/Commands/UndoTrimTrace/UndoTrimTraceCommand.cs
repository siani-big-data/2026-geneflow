using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UndoTrimTrace;

/// <summary>
/// Command to undo/remove trimming from a trace.
/// </summary>
public sealed record UndoTrimTraceCommand(
    string UserId,
    string TraceId) : ICommand<Result>;
