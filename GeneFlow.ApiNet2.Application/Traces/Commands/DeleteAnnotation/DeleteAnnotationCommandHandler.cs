using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.DeleteAnnotation;

/// <summary>
/// Handler for DeleteAnnotationCommand.
/// Removes an annotation from a trace.
/// </summary>
public sealed class DeleteAnnotationCommandHandler
    : ICommandHandler<DeleteAnnotationCommand, Result>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public DeleteAnnotationCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteAnnotationCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(TraceErrors.InvalidUserId);

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure(TraceErrors.NotFound);

        // Parse annotation ID
        if (!Guid.TryParse(request.AnnotationId, out var annotationId))
            return Result.Failure(TraceErrors.AnnotationNotFound);

        // Get trace with annotations collection loaded
        var trace = await _unitOfWork.Traces.GetByIdWithAnnotationsAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure(TraceErrors.NotFound);

        // Verify annotation exists
        var annotation = trace.Annotations.FirstOrDefault(a => a.Id == annotationId);
        if (annotation is null)
            return Result.Failure(TraceErrors.AnnotationNotFound);

        // Check if trace can be edited
        if (!trace.CanBeEdited)
            return Result.Failure(TraceErrors.CannotEditInCurrentStatus);

        // Delete annotation directly via SQL (avoids EF Core owned entity tracking issues)
        await _unitOfWork.Traces.DeleteAnnotationAsync(traceId, annotationId, cancellationToken);

        return Result.Success();
    }
}
