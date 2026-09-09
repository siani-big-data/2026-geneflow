using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.AddPipelineStep;

/// <summary>
/// Handler for AddPipelineStepCommand.
/// </summary>
public sealed class AddPipelineStepCommandHandler
    : ICommandHandler<AddPipelineStepCommand, Result<PipelineStepDto>>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;

    public AddPipelineStepCommandHandler(
        IPipelineRepository pipelineRepository,
        IPipelineUnitOfWork unitOfWork)
    {
        _pipelineRepository = pipelineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PipelineStepDto>> Handle(
        AddPipelineStepCommand request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId == null)
            return Result.Failure<PipelineStepDto>(PipelineErrors.InvalidUserId);

        if (!PipelineId.TryParse(request.PipelineId, out var pipelineId) || pipelineId == null)
            return Result.Failure<PipelineStepDto>(PipelineErrors.NotFound);

        var pipeline = await _pipelineRepository.GetByIdWithStepsAsync(pipelineId, cancellationToken);
        if (pipeline == null)
            return Result.Failure<PipelineStepDto>(PipelineErrors.NotFound);

        var stepType = StepType.FromId(request.StepTypeId);
        if (stepType == null)
            return Result.Failure<PipelineStepDto>(PipelineErrors.InvalidStepType);

        var configResult = StepConfiguration.Create(request.Configuration, stepType);
        if (configResult.IsFailure)
            return Result.Failure<PipelineStepDto>(configResult.Error);

        var stepResult = pipeline.AddStep(
            stepType,
            configResult.Value,
            request.Label,
            request.IsEnabled,
            userId);

        if (stepResult.IsFailure)
            return Result.Failure<PipelineStepDto>(stepResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(stepResult.Value.ToDto());
    }
}
