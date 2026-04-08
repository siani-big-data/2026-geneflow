using GeneFlow.ApiNet2.Application.Plans.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Plans.Queries.GetAllPlans;

/// <summary>
/// Query to get all active plans.
/// </summary>
public sealed record GetAllPlansQuery : IQuery<Result<IReadOnlyList<PlanDto>>>;
