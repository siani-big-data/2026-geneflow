using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.CreateAnnotation;

/// <summary>
/// Handler for CreateAnnotationCommand.
/// Creates a new annotation on a trace.
/// </summary>
public sealed class CreateAnnotationCommandHandler
    : ICommandHandler<CreateAnnotationCommand, Result<AnnotationDto>>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public CreateAnnotationCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AnnotationDto>> Handle(
        CreateAnnotationCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<AnnotationDto>(TraceErrors.InvalidUserId);

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<AnnotationDto>(TraceErrors.NotFound);

        // Get annotation type
        var annotationType = AnnotationType.FromId(request.TypeId);
        if (annotationType is null)
            return Result.Failure<AnnotationDto>(TraceErrors.InvalidAnnotationType);

        // Get annotation strand
        var strand = AnnotationStrand.FromId(request.StrandId);
        if (strand is null)
            return Result.Failure<AnnotationDto>(TraceErrors.InvalidAnnotationStrand);

        // Get trace
        var trace = await _unitOfWork.Traces.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<AnnotationDto>(TraceErrors.NotFound);

        // Add annotation
        var annotationResult = trace.AddAnnotation(
            annotationType,
            request.Label,
            request.Description,
            request.StartPosition,
            request.EndPosition,
            strand,
            request.Color,
            request.IsShared,
            request.Metadata,
            userId);

        if (annotationResult.IsFailure)
            return Result.Failure<AnnotationDto>(annotationResult.Error);

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(annotationResult.Value.ToDto(traceId.Value.ToString()));
    }
}
