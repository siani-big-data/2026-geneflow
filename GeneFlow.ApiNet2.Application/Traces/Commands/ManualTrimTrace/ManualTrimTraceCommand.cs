using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.ManualTrimTrace;

/// <summary>
/// Command to manually trim a trace by specifying trim boundaries.
/// </summary>
public sealed record ManualTrimTraceCommand(
    string UserId,
    string TraceId,
    int Start5Prime,
    int End5Prime,
    int Start3Prime,
    int End3Prime) : ICommand<Result<TrimRegionDto>>;
