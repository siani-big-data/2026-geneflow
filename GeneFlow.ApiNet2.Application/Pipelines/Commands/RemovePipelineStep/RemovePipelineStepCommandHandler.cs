using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.RemovePipelineStep;

/// <summary>
/// Handler for RemovePipelineStepCommand.
/// </summary>
public sealed class RemovePipelineStepCommandHandler
    : ICommandHandler<RemovePipelineStepCommand, Result>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;

    public RemovePipelineStepCommandHandler(
        IPipelineRepository pipelineRepository,
        IPipelineUnitOfWork unitOfWork)
    {
        _pipelineRepository = pipelineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        RemovePipelineStepCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!UserId.TryParse(request.UserId, out var userId) || userId == null)
            return Result.Failure(PipelineErrors.InvalidUserId);

        if (!PipelineId.TryParse(request.PipelineId, out var pipelineId) || pipelineId == null)
            return Result.Failure(PipelineErrors.NotFound);

        if (!Guid.TryParse(request.StepId, out var stepId))
            return Result.Failure(PipelineErrors.StepNotFound);

        // Get pipeline with steps
        var pipeline = await _pipelineRepository.GetByIdWithStepsAsync(pipelineId, cancellationToken);
        if (pipeline == null)
            return Result.Failure(PipelineErrors.NotFound);

        // Remove step
        var removeResult = pipeline.RemoveStep(stepId, userId);
        if (removeResult.IsFailure)
            return removeResult;

        // Persist
        // Entity already tracked - no Update needed
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
