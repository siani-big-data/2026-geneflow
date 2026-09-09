using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetPipelineExecutions;

/// <summary>
/// Handler for GetPipelineExecutionsQuery.
/// </summary>
public sealed class GetPipelineExecutionsQueryHandler
    : IQueryHandler<GetPipelineExecutionsQuery, Result<PagedList<PipelineExecutionSummaryDto>>>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineExecutionRepository _executionRepository;

    public GetPipelineExecutionsQueryHandler(
        IPipelineRepository pipelineRepository,
        IPipelineExecutionRepository executionRepository)
    {
        _pipelineRepository = pipelineRepository;
        _executionRepository = executionRepository;
    }

    public async Task<Result<PagedList<PipelineExecutionSummaryDto>>> Handle(
        GetPipelineExecutionsQuery request,
        CancellationToken cancellationToken)
    {
        if (!PipelineId.TryParse(request.PipelineId, out var pipelineId) || pipelineId == null)
            return Result.Failure<PagedList<PipelineExecutionSummaryDto>>(PipelineErrors.NotFound);

        var pipeline = await _pipelineRepository.GetByIdAsync(pipelineId, cancellationToken);
        if (pipeline == null)
            return Result.Failure<PagedList<PipelineExecutionSummaryDto>>(PipelineErrors.NotFound);

        ExecutionStatus? status = null;
        if (request.StatusId.HasValue)
        {
            status = ExecutionStatus.FromId(request.StatusId.Value);
        }

        var executions = await _executionRepository.GetByPipelineAsync(
            pipelineId,
            request.PageNumber,
            request.PageSize,
            status,
            cancellationToken);

        var dtos = executions.Items.Select(e => e.ToSummaryDto(pipeline.Name.Value)).ToList();

        return Result.Success(PagedList<PipelineExecutionSummaryDto>.Create(
            dtos,
            request.PageNumber,
            request.PageSize,
            executions.TotalCount));
    }
}
