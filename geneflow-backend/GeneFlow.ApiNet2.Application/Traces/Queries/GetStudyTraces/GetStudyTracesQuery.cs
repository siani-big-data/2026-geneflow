using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetStudyTraces;

/// <summary>
/// Query to get traces for a study with pagination and filtering.
/// Requires membership in the study (allows public study access).
/// </summary>
public sealed record GetStudyTracesQuery(
    string UserId,
    string StudyId,
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    int? StatusId = null,
    int? FormatId = null,
    string? SortBy = null,
    bool SortDescending = true) : IQuery<Result<PagedList<TraceSummaryDto>>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
