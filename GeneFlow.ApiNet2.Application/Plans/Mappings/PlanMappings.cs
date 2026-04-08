using GeneFlow.ApiNet2.Application.Plans.DTOs;
using GeneFlow.ApiNet2.Domain.Plans;

namespace GeneFlow.ApiNet2.Application.Plans.Mappings;

/// <summary>
/// Extension methods for mapping Plan entities to DTOs.
/// </summary>
public static class PlanMappings
{
    /// <summary>
    /// Maps a Plan to PlanDto.
    /// </summary>
    public static PlanDto ToDto(this Plan plan)
    {
        return new PlanDto(
            plan.Id.Value,
            plan.Name.Value,
            plan.Description,
            plan.Pricing.MonthlyPrice,
            plan.Pricing.AnnualPrice,
            plan.Pricing.Currency,
            plan.Limits.MaxStudies,
            plan.Limits.MaxTracesPerMonth,
            plan.Limits.MaxMembersPerStudy,
            plan.Features.Select(f => f.Name).ToList(),
            plan.IsActive,
            plan.IsDefault,
            plan.IsFree,
            plan.DisplayOrder);
    }

    /// <summary>
    /// Maps a collection of Plans to DTOs.
    /// </summary>
    public static IReadOnlyList<PlanDto> ToDtos(this IEnumerable<Plan> plans)
    {
        return plans.Select(p => p.ToDto()).ToList();
    }
}
