using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.RetryTraceProcessing;

/// <summary>
/// Command to retry processing a failed trace.
/// </summary>
public sealed record RetryTraceProcessingCommand(
    string UserId,
    string TraceId) : ICommand<Result>;
