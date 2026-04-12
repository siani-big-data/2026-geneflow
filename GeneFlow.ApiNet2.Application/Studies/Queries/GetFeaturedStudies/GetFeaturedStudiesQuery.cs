using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetFeaturedStudies;

/// <summary>
/// Query to get featured studies.
/// </summary>
public sealed record GetFeaturedStudiesQuery(
    int Limit = 10) : IQuery<Result<IReadOnlyList<StudySummaryDto>>>;
