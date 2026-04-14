using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.FailTraceProcessing;

/// <summary>
/// Command to fail trace processing (Worker API).
/// </summary>
public sealed record FailTraceProcessingCommand(
    string TraceId,
    string Reason) : ICommand<Result>;
