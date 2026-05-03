using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetStudyPipelines;

/// <summary>
/// Handler for GetStudyPipelinesQuery.
/// </summary>
public sealed class GetStudyPipelinesQueryHandler
    : IQueryHandler<GetStudyPipelinesQuery, Result<PagedList<PipelineSummaryDto>>>
{
    private readonly IPipelineRepository _pipelineRepository;

    public GetStudyPipelinesQueryHandler(IPipelineRepository pipelineRepository)
    {
        _pipelineRepository = pipelineRepository;
    }

    public async Task<Result<PagedList<PipelineSummaryDto>>> Handle(
        GetStudyPipelinesQuery request,
        CancellationToken cancellationToken)
    {
        // Parse ID
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId == null)
            return Result.Failure<PagedList<PipelineSummaryDto>>(PipelineErrors.InvalidStudyId);

        // Parse status filter
        PipelineStatus? status = null;
        if (request.StatusId.HasValue)
        {
            status = PipelineStatus.FromId(request.StatusId.Value);
            if (status == null)
                return Result.Failure<PagedList<PipelineSummaryDto>>(PipelineErrors.InvalidStatus);
        }

        // Query
        var pipelines = await _pipelineRepository.GetByStudyAsync(
            studyId,
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            status,
            cancellationToken);

        // Map to DTOs
        var dtos = pipelines.Items.ToSummaryDtos();

        return Result.Success(PagedList<PipelineSummaryDto>.Create(
            dtos,
            request.PageNumber,
            request.PageSize,
            pipelines.TotalCount));
    }
}
