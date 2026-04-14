using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.AutoTrimTrace;

/// <summary>
/// Command to auto-trim a trace using quality-based algorithm (e.g., Mott).
/// </summary>
public sealed record AutoTrimTraceCommand(
    string UserId,
    string TraceId,
    int QualityThreshold = 20,
    int WindowSize = 10,
    int MinimumLength = 50) : ICommand<Result<TrimRegionDto>>;
