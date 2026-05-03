using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetTraceExecutions;

/// <summary>
/// Handler for GetTraceExecutionsQuery.
/// </summary>
public sealed class GetTraceExecutionsQueryHandler
    : IQueryHandler<GetTraceExecutionsQuery, Result<PagedList<PipelineExecutionSummaryDto>>>
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineExecutionRepository _executionRepository;

    public GetTraceExecutionsQueryHandler(
        IPipelineRepository pipelineRepository,
        IPipelineExecutionRepository executionRepository)
    {
        _pipelineRepository = pipelineRepository;
        _executionRepository = executionRepository;
    }

    public async Task<Result<PagedList<PipelineExecutionSummaryDto>>> Handle(
        GetTraceExecutionsQuery request,
        CancellationToken cancellationToken)
    {
        // Parse ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId == null)
            return Result.Failure<PagedList<PipelineExecutionSummaryDto>>(PipelineErrors.TraceNotProcessed);

        // Parse status filter
        ExecutionStatus? status = null;
        if (request.StatusId.HasValue)
        {
            status = ExecutionStatus.FromId(request.StatusId.Value);
        }

        // Query
        var executions = await _executionRepository.GetByTraceAsync(
            traceId,
            request.PageNumber,
            request.PageSize,
            status,
            cancellationToken);

        // Get pipeline names - we need to look them up
        var pipelineIds = executions.Items.Select(e => e.PipelineId).Distinct().ToList();
        var pipelineNames = new Dictionary<string, string>();

        foreach (var pid in pipelineIds)
        {
            var pipeline = await _pipelineRepository.GetByIdAsync(pid, cancellationToken);
            if (pipeline != null)
            {
                pipelineNames[pid.ToString()] = pipeline.Name.Value;
            }
        }

        // Map to DTOs
        var dtos = executions.Items.Select(e =>
            e.ToSummaryDto(pipelineNames.GetValueOrDefault(e.PipelineId.ToString(), "Unknown"))).ToList();

        return Result.Success(PagedList<PipelineExecutionSummaryDto>.Create(
            dtos,
            request.PageNumber,
            request.PageSize,
            executions.TotalCount));
    }
}
