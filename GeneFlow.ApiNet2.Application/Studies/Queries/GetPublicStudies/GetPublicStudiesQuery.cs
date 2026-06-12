using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetPublicStudies;

/// <summary>
/// Query to get published (public) studies.
/// </summary>
public sealed record GetPublicStudiesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    int? ResearchFieldId = null,
    IReadOnlyList<string>? Tags = null,
    string? SortBy = null,
    bool SortDescending = true) : IQuery<Result<PagedList<StudySummaryDto>>>;
