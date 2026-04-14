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

        // Get trace
        var trace = await _unitOfWork.Traces.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure(TraceErrors.NotFound);

        // Remove annotation
        var removeResult = trace.RemoveAnnotation(annotationId, userId);
        if (removeResult.IsFailure)
            return removeResult;

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
