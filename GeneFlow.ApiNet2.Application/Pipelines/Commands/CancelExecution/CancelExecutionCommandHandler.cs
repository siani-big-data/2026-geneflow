using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.CancelExecution;

/// <summary>
/// Handler for CancelExecutionCommand.
/// </summary>
public sealed class CancelExecutionCommandHandler
    : ICommandHandler<CancelExecutionCommand, Result<PipelineExecutionDto>>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineExecutionRepository _executionRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;

    public CancelExecutionCommandHandler(
        IPipelineRepository pipelineRepository,
        IPipelineExecutionRepository executionRepository,
        IPipelineUnitOfWork unitOfWork)
    {
        _pipelineRepository = pipelineRepository;
        _executionRepository = executionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PipelineExecutionDto>> Handle(
        CancelExecutionCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!UserId.TryParse(request.UserId, out var userId) || userId == null)
            return Result.Failure<PipelineExecutionDto>(PipelineErrors.InvalidUserId);

        if (!PipelineExecutionId.TryParse(request.ExecutionId, out var executionId) || executionId == null)
            return Result.Failure<PipelineExecutionDto>(PipelineErrors.ExecutionNotFound);

        // Get execution
        var execution = await _executionRepository.GetByIdWithStepsAsync(executionId, cancellationToken);
        if (execution == null)
            return Result.Failure<PipelineExecutionDto>(PipelineErrors.ExecutionNotFound);

        // Get pipeline name for DTO
        var pipeline = await _pipelineRepository.GetByIdAsync(execution.PipelineId, cancellationToken);
        var pipelineName = pipeline?.Name.Value ?? "Unknown";

        // Cancel
        var cancelResult = execution.Cancel();
        if (cancelResult.IsFailure)
            return Result.Failure<PipelineExecutionDto>(cancelResult.Error);

        // Persist
        _executionRepository.Update(execution);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(execution.ToDto(pipelineName));
    }
}
