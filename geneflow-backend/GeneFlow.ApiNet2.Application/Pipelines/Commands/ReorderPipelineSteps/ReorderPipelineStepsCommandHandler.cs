using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.ReorderPipelineSteps;

/// <summary>
/// Handler for ReorderPipelineStepsCommand.
/// </summary>
public sealed class ReorderPipelineStepsCommandHandler
    : ICommandHandler<ReorderPipelineStepsCommand, Result<PipelineDto>>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;

    public ReorderPipelineStepsCommandHandler(
        IPipelineRepository pipelineRepository,
        IPipelineUnitOfWork unitOfWork)
    {
        _pipelineRepository = pipelineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PipelineDto>> Handle(
        ReorderPipelineStepsCommand request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId == null)
            return Result.Failure<PipelineDto>(PipelineErrors.InvalidUserId);

        if (!PipelineId.TryParse(request.PipelineId, out var pipelineId) || pipelineId == null)
            return Result.Failure<PipelineDto>(PipelineErrors.NotFound);

        var stepIds = new List<Guid>();
        foreach (var stepIdStr in request.StepIds)
        {
            if (!Guid.TryParse(stepIdStr, out var stepId))
                return Result.Failure<PipelineDto>(PipelineErrors.StepNotFound);
            stepIds.Add(stepId);
        }

        var pipeline = await _pipelineRepository.GetByIdWithStepsAsync(pipelineId, cancellationToken);
        if (pipeline == null)
            return Result.Failure<PipelineDto>(PipelineErrors.NotFound);

        var reorderResult = pipeline.ReorderSteps(stepIds, userId);
        if (reorderResult.IsFailure)
            return Result.Failure<PipelineDto>(reorderResult.Error);

        // The (pipeline_id, order) unique index is not deferrable, so saving
        // the new order directly can collide with the previous values
        // mid-update. Park the orders in the negative range first, then
        // persist the final positive values.
        pipeline.ToggleParkedStepOrders();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        pipeline.ToggleParkedStepOrders();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(pipeline.ToDto());
    }
}
