using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetExecutionById;

/// <summary>
/// Handler for GetExecutionByIdQuery.
/// </summary>
public sealed class GetExecutionByIdQueryHandler
    : IQueryHandler<GetExecutionByIdQuery, Result<PipelineExecutionDto>>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineExecutionRepository _executionRepository;

    public GetExecutionByIdQueryHandler(
        IPipelineRepository pipelineRepository,
        IPipelineExecutionRepository executionRepository)
    {
        _pipelineRepository = pipelineRepository;
        _executionRepository = executionRepository;
    }

    public async Task<Result<PipelineExecutionDto>> Handle(
        GetExecutionByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!PipelineExecutionId.TryParse(request.ExecutionId, out var executionId) || executionId == null)
            return Result.Failure<PipelineExecutionDto>(PipelineErrors.ExecutionNotFound);

        var execution = await _executionRepository.GetByIdWithStepsAsync(executionId, cancellationToken);
        if (execution == null)
            return Result.Failure<PipelineExecutionDto>(PipelineErrors.ExecutionNotFound);

        var pipeline = await _pipelineRepository.GetByIdAsync(execution.PipelineId, cancellationToken);
        var pipelineName = pipeline?.Name.Value ?? "Unknown";

        return Result.Success(execution.ToDto(pipelineName));
    }
}
