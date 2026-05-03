using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.UpdatePipelineStep;

/// <summary>
/// Handler for UpdatePipelineStepCommand.
/// </summary>
public sealed class UpdatePipelineStepCommandHandler
    : ICommandHandler<UpdatePipelineStepCommand, Result<PipelineStepDto>>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;

    public UpdatePipelineStepCommandHandler(
        IPipelineRepository pipelineRepository,
        IPipelineUnitOfWork unitOfWork)
    {
        _pipelineRepository = pipelineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PipelineStepDto>> Handle(
        UpdatePipelineStepCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!UserId.TryParse(request.UserId, out var userId) || userId == null)
            return Result.Failure<PipelineStepDto>(PipelineErrors.InvalidUserId);

        if (!PipelineId.TryParse(request.PipelineId, out var pipelineId) || pipelineId == null)
            return Result.Failure<PipelineStepDto>(PipelineErrors.NotFound);

        if (!Guid.TryParse(request.StepId, out var stepId))
            return Result.Failure<PipelineStepDto>(PipelineErrors.StepNotFound);

        // Get pipeline with steps
        var pipeline = await _pipelineRepository.GetByIdWithStepsAsync(pipelineId, cancellationToken);
        if (pipeline == null)
            return Result.Failure<PipelineStepDto>(PipelineErrors.NotFound);

        // Find step to get its type
        var existingStep = pipeline.Steps.FirstOrDefault(s => s.Id == stepId);
        if (existingStep == null)
            return Result.Failure<PipelineStepDto>(PipelineErrors.StepNotFound);

        // Create configuration
        var configResult = StepConfiguration.Create(request.Configuration, existingStep.StepType);
        if (configResult.IsFailure)
            return Result.Failure<PipelineStepDto>(configResult.Error);

        // Update step
        var updateResult = pipeline.UpdateStep(
            stepId,
            configResult.Value,
            request.Label,
            request.IsEnabled,
            userId);

        if (updateResult.IsFailure)
            return Result.Failure<PipelineStepDto>(updateResult.Error);

        // Persist
        // Entity already tracked - no Update needed
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get updated step
        var updatedStep = pipeline.Steps.First(s => s.Id == stepId);
        return Result.Success(updatedStep.ToDto());
    }
}
