using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.DeactivatePipeline;

/// <summary>
/// Handler for DeactivatePipelineCommand.
/// </summary>
public sealed class DeactivatePipelineCommandHandler
    : ICommandHandler<DeactivatePipelineCommand, Result<PipelineDto>>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;

    public DeactivatePipelineCommandHandler(
        IPipelineRepository pipelineRepository,
        IPipelineUnitOfWork unitOfWork)
    {
        _pipelineRepository = pipelineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PipelineDto>> Handle(
        DeactivatePipelineCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!UserId.TryParse(request.UserId, out var userId) || userId == null)
            return Result.Failure<PipelineDto>(PipelineErrors.InvalidUserId);

        if (!PipelineId.TryParse(request.PipelineId, out var pipelineId) || pipelineId == null)
            return Result.Failure<PipelineDto>(PipelineErrors.NotFound);

        // Get pipeline with steps
        var pipeline = await _pipelineRepository.GetByIdWithStepsAsync(pipelineId, cancellationToken);
        if (pipeline == null)
            return Result.Failure<PipelineDto>(PipelineErrors.NotFound);

        // Deactivate
        var deactivateResult = pipeline.Deactivate(userId);
        if (deactivateResult.IsFailure)
            return Result.Failure<PipelineDto>(deactivateResult.Error);

        // Persist
        // Entity already tracked - no Update needed
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(pipeline.ToDto());
    }
}
