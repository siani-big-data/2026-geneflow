using GeneFlow.ApiNet2.Application.Plans.DTOs;
using GeneFlow.ApiNet2.Application.Plans.Mappings;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Plans.Queries.GetPlanById;

/// <summary>
/// Handler for GetPlanByIdQuery.
/// </summary>
public sealed class GetPlanByIdQueryHandler
    : IQueryHandler<GetPlanByIdQuery, Result<PlanDto>>
{
    private readonly IPlanRepository _planRepository;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GetPlanByIdQueryHandler(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PlanDto>> Handle(
        GetPlanByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!PlanId.TryParse(request.PlanId, out var planId) || planId is null)
            return Result.Failure<PlanDto>(PlanErrors.NotFound);

        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
            return Result.Failure<PlanDto>(PlanErrors.NotFound);

        return plan.ToDto();
    }
}
