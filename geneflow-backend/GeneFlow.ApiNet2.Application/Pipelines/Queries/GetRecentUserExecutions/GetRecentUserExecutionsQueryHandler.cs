using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetRecentUserExecutions;

/// <summary>
/// Handler for <see cref="GetRecentUserExecutionsQuery"/>.
/// Resolves the user's studies via the Studies bounded context, then asks the
/// pipeline repository for the most recent executions across those studies.
/// </summary>
public sealed class GetRecentUserExecutionsQueryHandler
    : IQueryHandler<GetRecentUserExecutionsQuery,
        Result<IReadOnlyList<RecentPipelineExecutionDto>>>
{
    // Page size used to fetch the user's studies. The endpoint exposes a small
    // limit (default 5) on executions, so a single page of studies is enough
    // for the dashboard's recent-pipelines panel.
    private const int UserStudiesPageSize = 200;

    private readonly IPipelineExecutionRepository _executionRepository;
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IStudyRepository _studyRepository;

    public GetRecentUserExecutionsQueryHandler(
        IPipelineExecutionRepository executionRepository,
        IPipelineRepository pipelineRepository,
        IStudyRepository studyRepository)
    {
        _executionRepository = executionRepository;
        _pipelineRepository = pipelineRepository;
        _studyRepository = studyRepository;
    }

    public async Task<Result<IReadOnlyList<RecentPipelineExecutionDto>>> Handle(
        GetRecentUserExecutionsQuery request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
        {
            return Result.Failure<IReadOnlyList<RecentPipelineExecutionDto>>(
                PipelineErrors.InvalidUserId);
        }

        var limit = request.Limit <= 0 ? 5 : request.Limit;

        var studies = await _studyRepository.GetByMemberAsync(
            userId,
            pageNumber: 1,
            pageSize: UserStudiesPageSize,
            cancellationToken: cancellationToken);

        if (studies.Items.Count == 0)
        {
            return Result.Success<IReadOnlyList<RecentPipelineExecutionDto>>(
                Array.Empty<RecentPipelineExecutionDto>());
        }

        var studyTitlesById = studies.Items.ToDictionary(
            s => s.Id,
            s => s.Title.Value);

        var executions = await _executionRepository.GetRecentByStudiesAsync(
            studyTitlesById.Keys.ToList(),
            limit,
            cancellationToken);

        if (executions.Count == 0)
        {
            return Result.Success<IReadOnlyList<RecentPipelineExecutionDto>>(
                Array.Empty<RecentPipelineExecutionDto>());
        }

        // Load each pipeline once to resolve its name and study.
        var pipelineIds = executions
            .Select(e => e.PipelineId)
            .Distinct()
            .ToList();

        var pipelinesById = new Dictionary<PipelineId, (string Name, StudyId StudyId)>();
        foreach (var pipelineId in pipelineIds)
        {
            var pipeline = await _pipelineRepository.GetByIdAsync(pipelineId, cancellationToken);
            if (pipeline is not null)
            {
                pipelinesById[pipelineId] = (pipeline.Name.Value, pipeline.StudyId);
            }
        }

        var dtos = new List<RecentPipelineExecutionDto>(executions.Count);
        foreach (var execution in executions)
        {
            if (!pipelinesById.TryGetValue(execution.PipelineId, out var pipelineInfo))
            {
                continue;
            }

            if (!studyTitlesById.TryGetValue(pipelineInfo.StudyId, out var studyTitle))
            {
                continue;
            }

            dtos.Add(new RecentPipelineExecutionDto
            {
                Id = execution.Id.ToString(),
                PipelineId = execution.PipelineId.ToString(),
                PipelineName = pipelineInfo.Name,
                TraceId = execution.TraceId.ToString(),
                StudyId = pipelineInfo.StudyId.ToString(),
                StudyTitle = studyTitle,
                StatusId = execution.Status.Id,
                StatusName = execution.Status.DisplayName,
                TotalSteps = execution.TotalSteps,
                CompletedSteps = execution.CompletedSteps,
                ProgressPercentage = execution.ProgressPercentage,
                CreatedAt = execution.CreatedAt,
                CompletedAt = execution.CompletedAt,
                DurationSeconds = execution.Duration?.TotalSeconds,
            });
        }

        return Result.Success<IReadOnlyList<RecentPipelineExecutionDto>>(dtos);
    }
}
