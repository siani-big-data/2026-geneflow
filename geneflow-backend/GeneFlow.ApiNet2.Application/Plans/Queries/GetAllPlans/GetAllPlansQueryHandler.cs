using GeneFlow.ApiNet2.Application.Plans.DTOs;
using GeneFlow.ApiNet2.Application.Plans.Mappings;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Plans.Queries.GetAllPlans;

/// <summary>
/// Handler for GetAllPlansQuery.
/// </summary>
public sealed class GetAllPlansQueryHandler
    : IQueryHandler<GetAllPlansQuery, Result<IReadOnlyList<PlanDto>>>
{
    private readonly IPlanRepository _planRepository;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GetAllPlansQueryHandler(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PlanDto>>> Handle(
        GetAllPlansQuery request,
        CancellationToken cancellationToken)
    {
        var plans = await _planRepository.GetAllActiveAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PlanDto>>(plans.ToDtos());
    }
}
