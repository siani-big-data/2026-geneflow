using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetUserStudies;

/// <summary>
/// Query to get all studies where the user is a member.
/// Requires authentication.
/// </summary>
public sealed record GetUserStudiesQuery(
    string UserId,
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    int? StatusId = null,
    int? ResearchFieldId = null) : IQuery<Result<PagedList<StudySummaryDto>>>, IRequireAuthentication;
