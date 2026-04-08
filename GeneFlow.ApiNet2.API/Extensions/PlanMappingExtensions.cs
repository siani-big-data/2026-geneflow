using GeneFlow.ApiNet2.API.Contracts.Plans.Responses;
using GeneFlow.ApiNet2.Application.Plans.DTOs;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Extension methods for mapping Plan DTOs to API responses.
/// </summary>
public static class PlanMappingExtensions
{
    /// <summary>
    /// Maps a PlanDto to PlanResponse.
    /// </summary>
    public static PlanResponse ToResponse(this PlanDto dto)
    {
        return new PlanResponse(
            dto.PlanId,
            dto.Name,
            dto.Description,
            new PlanPricingResponse(dto.MonthlyPrice, dto.AnnualPrice, dto.Currency),
            new PlanLimitsResponse(dto.MaxStudies, dto.MaxTracesPerMonth, dto.MaxMembersPerStudy),
            dto.Features,
            dto.IsActive,
            dto.IsDefault,
            dto.IsFree,
            dto.DisplayOrder);
    }

    /// <summary>
    /// Maps a collection of PlanDtos to responses.
    /// </summary>
    public static IReadOnlyList<PlanResponse> ToResponses(this IEnumerable<PlanDto> dtos)
    {
        return dtos.Select(d => d.ToResponse()).ToList();
    }
}
