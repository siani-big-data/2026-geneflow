using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Application.Pipelines.Mappings;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetPipelineById;

/// <summary>
/// Handler for GetPipelineByIdQuery.
/// </summary>
public sealed class GetPipelineByIdQueryHandler
    : IQueryHandler<GetPipelineByIdQuery, Result<PipelineDto>>
{
    private readonly IPipelineRepository _pipelineRepository;

    public GetPipelineByIdQueryHandler(IPipelineRepository pipelineRepository)
    {
        _pipelineRepository = pipelineRepository;
    }

    public async Task<Result<PipelineDto>> Handle(
        GetPipelineByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!PipelineId.TryParse(request.PipelineId, out var pipelineId) || pipelineId == null)
            return Result.Failure<PipelineDto>(PipelineErrors.NotFound);

        var pipeline = await _pipelineRepository.GetByIdWithStepsAsync(pipelineId, cancellationToken);
        if (pipeline == null)
            return Result.Failure<PipelineDto>(PipelineErrors.NotFound);

        return Result.Success(pipeline.ToDto());
    }
}
