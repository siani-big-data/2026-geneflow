using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using System.Text.Json;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.CreateAnnotation;

/// <summary>
/// Command to create an annotation on a trace.
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
    JsonDocument? Metadata = null) : ICommand<Result<AnnotationDto>>;
