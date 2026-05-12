using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using MediatR;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.CompletePipelineExecution;

/// <summary>
/// Handler for CompletePipelineExecutionCommand.
/// </summary>
public sealed class CompletePipelineExecutionCommandHandler
    : ICommandHandler<CompletePipelineExecutionCommand, Result>
{
    private readonly IPipelineExecutionRepository _executionRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public CompletePipelineExecutionCommandHandler(
        IPipelineExecutionRepository executionRepository,
        IPipelineUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _executionRepository = executionRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(
        CompletePipelineExecutionCommand request,
        CancellationToken cancellationToken)
    {
        if (!PipelineExecutionId.TryParse(request.ExecutionId, out var executionId) || executionId == null)
            return Result.Failure(PipelineErrors.ExecutionNotFound);

        var execution = await _executionRepository.GetByIdWithStepsAsync(executionId, cancellationToken);
        if (execution == null)
            return Result.Failure(PipelineErrors.ExecutionNotFound);

        var completeResult = execution.Complete();
        if (completeResult.IsFailure)
            return completeResult;

        _executionRepository.Update(execution);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new PipelineExecutionCompletedEvent(
                executionId,
                execution.PipelineId,
                execution.TraceId,
                execution.CompletedSteps,
                execution.Duration ?? TimeSpan.Zero),
            cancellationToken);

        return Result.Success();
    }
}
