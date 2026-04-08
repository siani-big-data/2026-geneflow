namespace GeneFlow.ApiNet2.API.Contracts.Plans.Responses;

/// <summary>
/// Response for a plan.
/// </summary>
public sealed record PlanResponse(
    Guid Id,
    string Name,
    string? Description,
    PlanPricingResponse Pricing,
    PlanLimitsResponse Limits,
    IReadOnlyList<string> Features,
    bool IsActive,
    bool IsDefault,
    bool IsFree,
    int DisplayOrder);

/// <summary>
/// Response for plan pricing.
/// </summary>
public sealed record PlanPricingResponse(
    decimal MonthlyPrice,
    decimal AnnualPrice,
    string Currency);

/// <summary>
/// Response for plan limits.
/// </summary>
public sealed record PlanLimitsResponse(
    int MaxStudies,
    int MaxTracesPerMonth,
    int MaxMembersPerStudy);
