namespace GeneFlow.ApiNet2.Application.Plans.DTOs;

/// <summary>
/// DTO for Plan aggregate.
/// </summary>
public sealed record PlanDto(
    string PlanId,
    string Name,
    string? Description,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    string Currency,
    int MaxStudies,
    int MaxTracesPerMonth,
    int MaxMembersPerStudy,
    IReadOnlyList<string> Features,
    bool IsActive,
    bool IsDefault,
    bool IsFree,
    int DisplayOrder);

/// <summary>
/// DTO for Plan pricing.
/// </summary>
public sealed record PlanPricingDto(
    decimal MonthlyPrice,
    decimal AnnualPrice,
    string Currency);

/// <summary>
/// DTO for Plan limits.
/// </summary>
public sealed record PlanLimitsDto(
    int MaxStudies,
    int MaxTracesPerMonth,
    int MaxMembersPerStudy);
