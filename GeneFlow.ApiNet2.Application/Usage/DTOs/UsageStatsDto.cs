namespace GeneFlow.ApiNet2.Application.Usage.DTOs;

/// <summary>
/// DTO for user usage statistics.
/// </summary>
public sealed record UsageStatsDto(
    string UserId,
    string PeriodKey,
    UsageCountsDto Counts,
    UsageLimitsDto Limits,
    DateTime? LastActivityAt,
    DateTime UpdatedAt);

/// <summary>
/// DTO for usage counts.
/// </summary>
public sealed record UsageCountsDto(
    int StudiesOwned,
    int StudiesTotal,
    int TracesThisPeriod,
    long TracesTotal,
    int MaxMembersInStudy,
    int AlignmentsThisPeriod,
    long AlignmentsTotal,
    long AlignmentsCompleted,
    int TracesPending);

/// <summary>
/// DTO for usage limits (from the user's current plan).
/// </summary>
public sealed record UsageLimitsDto(
    int MaxStudies,
    int MaxTracesPerMonth,
    int MaxMembersPerStudy);

/// <summary>
/// DTO for billing page usage summary.
/// </summary>
public sealed record BillingUsageDto(
    UsageItemDto Studies,
    UsageItemDto Traces,
    UsageItemDto Members,
    BillingPeriodDto Period);

/// <summary>
/// DTO for a single usage item with used/total/percentage.
/// </summary>
public sealed record UsageItemDto(
    int Used,
    int Total,
    int Percentage);

/// <summary>
/// DTO for billing period info.
/// </summary>
public sealed record BillingPeriodDto(
    DateTime StartDate,
    DateTime EndDate,
    int DaysRemaining,
    int TotalDays);

/// <summary>
/// DTO for dashboard statistics.
/// </summary>
public sealed record DashboardStatsDto(
    int ActiveStudies,
    long ProcessedTraces,
    int PendingTraces,
    int TeamActivity,
    int AlignmentsCompleted);
