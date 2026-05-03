using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.DeletePipeline;

/// <summary>
/// Handler for DeletePipelineCommand.
/// </summary>
public sealed class DeletePipelineCommandHandler
    : ICommandHandler<DeletePipelineCommand, Result>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;

    public DeletePipelineCommandHandler(
        IPipelineRepository pipelineRepository,
        IPipelineUnitOfWork unitOfWork)
    {
        _pipelineRepository = pipelineRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeletePipelineCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!UserId.TryParse(request.UserId, out var userId) || userId == null)
            return Result.Failure(PipelineErrors.InvalidUserId);

        if (!PipelineId.TryParse(request.PipelineId, out var pipelineId) || pipelineId == null)
            return Result.Failure(PipelineErrors.NotFound);

        // Get pipeline
        var pipeline = await _pipelineRepository.GetByIdAsync(pipelineId, cancellationToken);
        if (pipeline == null)
            return Result.Failure(PipelineErrors.NotFound);

        // Check if can be deleted
        if (!pipeline.CanBeDeleted)
            return Result.Failure(PipelineErrors.PipelineNotDeletable);

        // Delete
        _pipelineRepository.Delete(pipeline);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
