using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetStudyTraces;

/// <summary>
/// Handler for GetStudyTracesQuery.
/// </summary>
public sealed class GetStudyTracesQueryHandler
    : IQueryHandler<GetStudyTracesQuery, Result<PagedList<TraceSummaryDto>>>
{
    private readonly ITraceRepository _traceRepository;

    public GetStudyTracesQueryHandler(ITraceRepository traceRepository)
    {
        _traceRepository = traceRepository;
    }

    public async Task<Result<PagedList<TraceSummaryDto>>> Handle(
        GetStudyTracesQuery request,
        CancellationToken cancellationToken)
    {
        // Parse user ID (for access control, if needed in the future)
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<PagedList<TraceSummaryDto>>(TraceErrors.InvalidUserId);

        // Parse study ID
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<PagedList<TraceSummaryDto>>(TraceErrors.InvalidStudyId);

        // Parse optional status filter
        TraceStatus? status = null;
        if (request.StatusId.HasValue)
        {
            status = TraceStatus.FromId(request.StatusId.Value);
            if (status is null)
                return Result.Failure<PagedList<TraceSummaryDto>>(TraceErrors.InvalidStatus);
        }

        // Parse optional format filter
        TraceFormat? format = null;
        if (request.FormatId.HasValue)
        {
            format = TraceFormat.FromId(request.FormatId.Value);
        }

        // Get traces
        var traces = await _traceRepository.GetByStudyAsync(
            studyId,
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            status,
            format,
            request.SortBy,
            request.SortDescending,
            cancellationToken);

        // Map to DTOs
        var summaryDtos = traces.Items.ToSummaryDtos();

        return Result.Success(PagedList<TraceSummaryDto>.Create(
            summaryDtos,
            traces.PageNumber,
            traces.PageSize,
            traces.TotalCount));
    }
}
