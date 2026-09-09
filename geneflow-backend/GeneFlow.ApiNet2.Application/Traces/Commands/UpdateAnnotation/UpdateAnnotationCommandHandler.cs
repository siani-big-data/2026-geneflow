using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UpdateAnnotation;

/// <summary>
/// Handler for UpdateAnnotationCommand.
/// Updates an existing annotation on a trace.
/// </summary>
public sealed class UpdateAnnotationCommandHandler
    : ICommandHandler<UpdateAnnotationCommand, Result<AnnotationDto>>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public UpdateAnnotationCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AnnotationDto>> Handle(
        UpdateAnnotationCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<AnnotationDto>(TraceErrors.InvalidUserId);

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<AnnotationDto>(TraceErrors.NotFound);

        // Parse annotation ID
        if (!Guid.TryParse(request.AnnotationId, out var annotationId))
            return Result.Failure<AnnotationDto>(TraceErrors.AnnotationNotFound);

        // Get annotation strand
        var strand = AnnotationStrand.FromId(request.StrandId);
        if (strand is null)
            return Result.Failure<AnnotationDto>(TraceErrors.InvalidAnnotationStrand);

        // Get trace with annotations collection loaded for proper change tracking
        var trace = await _unitOfWork.Traces.GetByIdWithAnnotationsAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<AnnotationDto>(TraceErrors.NotFound);

        // Update annotation
        var updateResult = trace.UpdateAnnotation(
            annotationId,
            request.Label,
            request.Description,
            request.StartPosition,
            request.EndPosition,
            strand,
            request.Color,
            request.IsShared,
            request.Metadata,
            userId);

        if (updateResult.IsFailure)
            return Result.Failure<AnnotationDto>(updateResult.Error);

        // Get updated annotation
        var annotation = trace.GetAnnotation(annotationId);
        if (annotation is null)
            return Result.Failure<AnnotationDto>(TraceErrors.AnnotationNotFound);

        // Persist - no need to call Update() since trace is already tracked
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(annotation.ToDto(traceId.Value.ToString()));
    }
}
