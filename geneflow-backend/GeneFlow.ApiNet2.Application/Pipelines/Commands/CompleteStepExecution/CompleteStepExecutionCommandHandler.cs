using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using MediatR;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.CompleteStepExecution;

/// <summary>
/// Handler for CompleteStepExecutionCommand.
/// </summary>
public sealed class CompleteStepExecutionCommandHandler
    : ICommandHandler<CompleteStepExecutionCommand, Result>
{
    private readonly IPipelineExecutionRepository _executionRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public CompleteStepExecutionCommandHandler(
        IPipelineExecutionRepository executionRepository,
        IPipelineUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _executionRepository = executionRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(
        CompleteStepExecutionCommand request,
        CancellationToken cancellationToken)
    {
        if (!PipelineExecutionId.TryParse(request.ExecutionId, out var executionId) || executionId == null)
            return Result.Failure(PipelineErrors.ExecutionNotFound);

        if (!Guid.TryParse(request.StepExecutionId, out var stepExecutionId))
            return Result.Failure(PipelineErrors.StepExecutionNotFound);

        var execution = await _executionRepository.GetByIdWithStepsAsync(executionId, cancellationToken);
        if (execution == null)
            return Result.Failure(PipelineErrors.ExecutionNotFound);

        var stepExecution = execution.GetStepExecution(stepExecutionId);
        if (stepExecution == null)
            return Result.Failure(PipelineErrors.StepExecutionNotFound);

        var completeResult = stepExecution.Complete(request.ResultSummary, request.ResultData);
        if (completeResult.IsFailure)
            return completeResult;

        execution.IncrementCompletedSteps();

        _executionRepository.Update(execution);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new PipelineStepCompletedEvent(
                executionId,
                stepExecutionId,
                stepExecution.StepType,
                stepExecution.Order,
                true,
                stepExecution.Duration ?? TimeSpan.Zero),
            cancellationToken);

        return Result.Success();
    }
}
