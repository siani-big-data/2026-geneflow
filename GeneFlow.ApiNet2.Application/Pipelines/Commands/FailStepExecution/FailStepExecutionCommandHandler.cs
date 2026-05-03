using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using MediatR;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.FailStepExecution;

/// <summary>
/// Handler for FailStepExecutionCommand.
/// </summary>
public sealed class FailStepExecutionCommandHandler
    : ICommandHandler<FailStepExecutionCommand, Result>
{
    private readonly IPipelineExecutionRepository _executionRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public FailStepExecutionCommandHandler(
        IPipelineExecutionRepository executionRepository,
        IPipelineUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _executionRepository = executionRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(
        FailStepExecutionCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!PipelineExecutionId.TryParse(request.ExecutionId, out var executionId) || executionId == null)
            return Result.Failure(PipelineErrors.ExecutionNotFound);

        if (!Guid.TryParse(request.StepExecutionId, out var stepExecutionId))
            return Result.Failure(PipelineErrors.StepExecutionNotFound);

        // Get execution with steps
        var execution = await _executionRepository.GetByIdWithStepsAsync(executionId, cancellationToken);
        if (execution == null)
            return Result.Failure(PipelineErrors.ExecutionNotFound);

        // Find step execution
        var stepExecution = execution.GetStepExecution(stepExecutionId);
        if (stepExecution == null)
            return Result.Failure(PipelineErrors.StepExecutionNotFound);

        // Fail step
        var failResult = stepExecution.Fail(request.ErrorMessage);
        if (failResult.IsFailure)
            return failResult;

        // Also fail the entire execution
        execution.Fail(request.ErrorMessage);

        // Persist
        _executionRepository.Update(execution);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish step event
        await _publisher.Publish(
            new PipelineStepCompletedEvent(
                executionId,
                stepExecutionId,
                stepExecution.StepType,
                stepExecution.Order,
                false,
                stepExecution.Duration ?? TimeSpan.Zero),
            cancellationToken);

        // Publish execution failed event
        await _publisher.Publish(
            new PipelineExecutionFailedEvent(
                executionId,
                execution.PipelineId,
                execution.TraceId,
                execution.CompletedSteps,
                request.ErrorMessage),
            cancellationToken);

        return Result.Success();
    }
}
