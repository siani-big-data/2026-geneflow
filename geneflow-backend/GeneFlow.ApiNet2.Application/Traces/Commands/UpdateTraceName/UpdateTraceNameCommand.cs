using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UpdateTraceName;

/// <summary>
/// Command to update a trace's name.
/// Requires Editor role or higher in the trace's parent study.
/// </summary>
public sealed record UpdateTraceNameCommand(
    string UserId,
    string TraceId,
    string Name) : ICommand<Result>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
    StudyRole? IRequireTraceAccess.MinimumRole => StudyRole.Editor;
}
