using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.DeleteTrace;

/// <summary>
/// Command to delete a trace (soft delete).
/// </summary>
public sealed record DeleteTraceCommand(
    string UserId,
    string TraceId) : ICommand<Result>;
