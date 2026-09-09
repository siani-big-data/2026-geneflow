using System.Text.Json;
using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.CreateAnnotation;

/// <summary>
/// Command to create an annotation on a trace.
/// Requires Editor role or higher in the trace's parent study.
/// </summary>
public sealed record CreateAnnotationCommand(
    string UserId,
    string TraceId,
    int TypeId,
    string Label,
    string? Description,
    int StartPosition,
    int EndPosition,
    int StrandId,
    string Color,
    bool IsShared,
    JsonDocument? Metadata = null) : ICommand<Result<AnnotationDto>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
    StudyRole? IRequireTraceAccess.MinimumRole => StudyRole.Editor;
}
