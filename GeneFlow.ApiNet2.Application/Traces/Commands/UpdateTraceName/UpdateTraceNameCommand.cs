using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UpdateTraceName;

/// <summary>
/// Command to update a trace's name.
/// </summary>
public sealed record UpdateTraceNameCommand(
    string UserId,
    string TraceId,
    string Name) : ICommand<Result>;
