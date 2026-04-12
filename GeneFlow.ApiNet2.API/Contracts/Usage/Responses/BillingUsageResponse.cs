namespace GeneFlow.ApiNet2.API.Contracts.Usage.Responses;

/// <summary>
/// Response for billing usage statistics.
/// </summary>
public sealed record BillingUsageResponse(
    UsageItemResponse Studies,
    UsageItemResponse Traces,
    UsageItemResponse Members,
    BillingPeriodResponse Period);

/// <summary>
/// Response for a single usage item.
/// </summary>
public sealed record UsageItemResponse(
    int Used,
    int Total,
    int Percentage);

/// <summary>
/// Response for billing period info.
/// </summary>
public sealed record BillingPeriodResponse(
    DateTime StartDate,
    DateTime EndDate,
    int DaysRemaining,
    int TotalDays);

/// <summary>
/// Response for dashboard statistics.
/// </summary>
public sealed record DashboardStatsResponse(
    int ActiveStudies,
    long ProcessedTraces,
    int PendingTraces,
    int TeamActivity,
    int AlignmentsCompleted);
