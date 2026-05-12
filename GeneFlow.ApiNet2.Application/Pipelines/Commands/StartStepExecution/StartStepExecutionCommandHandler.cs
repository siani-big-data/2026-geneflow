using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.StartStepExecution;

/// <summary>
/// Handler for StartStepExecutionCommand.
/// </summary>
public sealed class StartStepExecutionCommandHandler
    : ICommandHandler<StartStepExecutionCommand, Result>
{
    private readonly IPipelineExecutionRepository _executionRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;

    public StartStepExecutionCommandHandler(
        IPipelineExecutionRepository executionRepository,
        IPipelineUnitOfWork unitOfWork)
    {
        _executionRepository = executionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        StartStepExecutionCommand request,
        CancellationToken cancellationToken)
    {
        if (!PipelineExecutionId.TryParse(request.ExecutionId, out var executionId) || executionId == null)
            return Result.Failure(PipelineErrors.ExecutionNotFound);

        if (!Guid.TryParse(request.StepExecutionId, out var stepExecutionId))
            return Result.Failure(PipelineErrors.StepExecutionNotFound);

        var execution = await _executionRepository.GetByIdWithStepsAsync(executionId, cancellationToken);
        if (execution == null)
            return Result.Failure(PipelineErrors.ExecutionNotFound);

        if (execution.Status == ExecutionStatus.Pending)
        {
            var startResult = execution.Start();
            if (startResult.IsFailure)
                return startResult;
        }

        var stepExecution = execution.GetStepExecution(stepExecutionId);
        if (stepExecution == null)
            return Result.Failure(PipelineErrors.StepExecutionNotFound);

        var stepStartResult = stepExecution.Start();
        if (stepStartResult.IsFailure)
            return stepStartResult;

        _executionRepository.Update(execution);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
