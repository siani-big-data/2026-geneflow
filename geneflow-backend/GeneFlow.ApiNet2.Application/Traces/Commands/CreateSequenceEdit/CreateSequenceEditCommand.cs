using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.CreateSequenceEdit;

/// <summary>
/// Command to create a sequence edit on a trace.
/// Requires Editor role or higher in the trace's parent study.
/// </summary>
public sealed record CreateSequenceEditCommand(
    string UserId,
    string TraceId,
    int EditType,
    int Position,
    char? OriginalBase,
    char? NewBase,
    string? Reason) : ICommand<Result<SequenceEditDto>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
    StudyRole? IRequireTraceAccess.MinimumRole => StudyRole.Editor;
}
