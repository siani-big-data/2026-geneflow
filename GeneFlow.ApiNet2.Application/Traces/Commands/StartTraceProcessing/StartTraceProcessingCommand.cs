using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.StartTraceProcessing;

/// <summary>
/// Command to start processing a trace (Worker API).
/// </summary>
public sealed record StartTraceProcessingCommand(
    string TraceId) : ICommand<Result>;
