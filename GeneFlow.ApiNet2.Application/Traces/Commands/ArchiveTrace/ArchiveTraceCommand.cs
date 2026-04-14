using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.ArchiveTrace;

/// <summary>
/// Command to archive a trace.
/// </summary>
public sealed record ArchiveTraceCommand(
    string UserId,
    string TraceId) : ICommand<Result>;
