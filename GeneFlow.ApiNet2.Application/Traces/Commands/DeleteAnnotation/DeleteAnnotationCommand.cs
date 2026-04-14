using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.DeleteAnnotation;

/// <summary>
/// Command to delete an annotation from a trace.
/// </summary>
public sealed record DeleteAnnotationCommand(
    string UserId,
    string TraceId,
    string AnnotationId) : ICommand<Result>;
