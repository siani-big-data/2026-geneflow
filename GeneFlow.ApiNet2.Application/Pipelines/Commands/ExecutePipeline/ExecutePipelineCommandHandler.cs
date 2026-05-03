using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.ExecutePipeline;

/// <summary>
/// Handler for ExecutePipelineCommand.
/// </summary>
public sealed class ExecutePipelineCommandHandler
    : ICommandHandler<ExecutePipelineCommand, Result<PipelineExecutionDto>>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineExecutionRepository _executionRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;
    private readonly ITraceRepository _traceRepository;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly IJobPublisher _jobPublisher;

    public ExecutePipelineCommandHandler(
        IPipelineRepository pipelineRepository,
        IPipelineExecutionRepository executionRepository,
        IPipelineUnitOfWork unitOfWork,
        ITraceRepository traceRepository,
        ISequenceGenerator sequenceGenerator,
        IJobPublisher jobPublisher)
    {
        _pipelineRepository = pipelineRepository;
        _executionRepository = executionRepository;
        _unitOfWork = unitOfWork;
        _traceRepository = traceRepository;
        _sequenceGenerator = sequenceGenerator;
        _jobPublisher = jobPublisher;
    }

    public async Task<Result<PipelineExecutionDto>> Handle(
        ExecutePipelineCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!UserId.TryParse(request.UserId, out var userId) || userId == null)
            return Result.Failure<PipelineExecutionDto>(PipelineErrors.InvalidUserId);

        if (!PipelineId.TryParse(request.PipelineId, out var pipelineId) || pipelineId == null)
            return Result.Failure<PipelineExecutionDto>(PipelineErrors.NotFound);

        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId == null)
            return Result.Failure<PipelineExecutionDto>(PipelineErrors.TraceNotProcessed);

        // Get pipeline with steps
        var pipeline = await _pipelineRepository.GetByIdWithStepsAsync(pipelineId, cancellationToken);
        if (pipeline == null)
            return Result.Failure<PipelineExecutionDto>(PipelineErrors.NotFound);

        // Check if trace exists and is processed
        var trace = await _traceRepository.GetByIdAsync(traceId, cancellationToken);
        if (trace == null)
            return Result.Failure<PipelineExecutionDto>(PipelineErrors.TraceNotProcessed);

        if (trace.Status != TraceStatus.Processed)
            return Result.Failure<PipelineExecutionDto>(PipelineErrors.TraceNotProcessed);

        // Check for already running execution
        if (await _executionRepository.HasRunningExecutionForTraceAsync(traceId, cancellationToken))
            return Result.Failure<PipelineExecutionDto>(PipelineErrors.TraceAlreadyRunning);

        // Generate execution ID
        var sequenceValue = await _sequenceGenerator.NextAsync(
            PipelineExecutionId.SequenceName,
            cancellationToken);
        var executionId = PipelineExecutionId.FromSequence(sequenceValue);

        // Start execution
        var executionResult = pipeline.StartExecution(executionId, traceId, userId);
        if (executionResult.IsFailure)
            return Result.Failure<PipelineExecutionDto>(executionResult.Error);

        var execution = executionResult.Value;

        // Persist
        await _executionRepository.AddAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish job to Redis
        var job = new PipelineJob(
            execution.Id.ToString(),
            pipeline.Id.ToString(),
            traceId.ToString(),
            execution.StepExecutions.Select(se => new PipelineStepJob(
                se.Id,
                se.Order,
                se.StepType.AnalysisKey,
                pipeline.Steps.First(s => s.Id == se.PipelineStepId).Configuration.ToDictionary()
            )).ToList());

        await _jobPublisher.PublishPipelineJobAsync(job, cancellationToken);

        return Result.Success(execution.ToDto(pipeline.Name.Value));
    }
}
