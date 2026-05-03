using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.UpdatePipeline;

/// <summary>
/// Handler for UpdatePipelineCommand.
/// </summary>
public sealed class UpdatePipelineCommandHandler
    : ICommandHandler<UpdatePipelineCommand, Result<PipelineDto>>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;

    public UpdatePipelineCommandHandler(
        IPipelineRepository pipelineRepository,
        IPipelineUnitOfWork unitOfWork)
    {
        _pipelineRepository = pipelineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PipelineDto>> Handle(
        UpdatePipelineCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!UserId.TryParse(request.UserId, out var userId) || userId == null)
            return Result.Failure<PipelineDto>(PipelineErrors.InvalidUserId);

        if (!PipelineId.TryParse(request.PipelineId, out var pipelineId) || pipelineId == null)
            return Result.Failure<PipelineDto>(PipelineErrors.NotFound);

        // Get pipeline
        var pipeline = await _pipelineRepository.GetByIdWithStepsAsync(pipelineId, cancellationToken);
        if (pipeline == null)
            return Result.Failure<PipelineDto>(PipelineErrors.NotFound);

        // Create value objects
        var nameResult = PipelineName.Create(request.Name);
        if (nameResult.IsFailure)
            return Result.Failure<PipelineDto>(nameResult.Error);

        var descriptionResult = PipelineDescription.Create(request.Description);
        if (descriptionResult.IsFailure)
            return Result.Failure<PipelineDto>(descriptionResult.Error);

        // Update
        var updateResult = pipeline.Update(nameResult.Value, descriptionResult.Value, userId);
        if (updateResult.IsFailure)
            return Result.Failure<PipelineDto>(updateResult.Error);

        // Persist
        // Entity already tracked - no Update needed
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(pipeline.ToDto());
    }
}
