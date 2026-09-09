using GeneFlow.ApiNet2.Application.Plans.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Plans.Queries.GetPlanById;

/// <summary>
/// Query to get a plan by ID.
/// </summary>
public sealed record GetPlanByIdQuery(string PlanId) : IQuery<Result<PlanDto>>;
