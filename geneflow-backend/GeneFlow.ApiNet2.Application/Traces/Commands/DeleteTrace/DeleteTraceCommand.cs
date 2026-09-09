using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.DeleteTrace;

/// <summary>
/// Command to delete a trace (soft delete).
/// Requires Admin role or higher in the trace's parent study.
/// </summary>
public sealed record DeleteTraceCommand(
    string UserId,
    string TraceId) : ICommand<Result>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
    StudyRole? IRequireTraceAccess.MinimumRole => StudyRole.Admin;
}
