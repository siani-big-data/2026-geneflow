using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetOrgStudies;

/// <summary>
/// Lists studies owned by an organisation (by org handle).
/// Members of the org see every non-deleted study; everyone else
/// only sees Published ones.
/// </summary>
public sealed record GetOrgStudiesQuery(
    string Handle,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<Result<PagedList<StudyDto>>>;
