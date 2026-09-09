using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.ActivatePipeline;

/// <summary>
/// Handler for ActivatePipelineCommand.
/// </summary>
public sealed class ActivatePipelineCommandHandler
    : ICommandHandler<ActivatePipelineCommand, Result<PipelineDto>>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;

    public ActivatePipelineCommandHandler(
        IPipelineRepository pipelineRepository,
        IPipelineUnitOfWork unitOfWork)
    {
        _pipelineRepository = pipelineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PipelineDto>> Handle(
        ActivatePipelineCommand request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId == null)
            return Result.Failure<PipelineDto>(PipelineErrors.InvalidUserId);

        if (!PipelineId.TryParse(request.PipelineId, out var pipelineId) || pipelineId == null)
            return Result.Failure<PipelineDto>(PipelineErrors.NotFound);

        var pipeline = await _pipelineRepository.GetByIdWithStepsAsync(pipelineId, cancellationToken);
        if (pipeline == null)
            return Result.Failure<PipelineDto>(PipelineErrors.NotFound);

        var activateResult = pipeline.Activate(userId);
        if (activateResult.IsFailure)
            return Result.Failure<PipelineDto>(activateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(pipeline.ToDto());
    }
}
