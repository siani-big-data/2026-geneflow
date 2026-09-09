namespace GeneFlow.ApiNet2.API.Contracts.Usage.Requests;

/// <summary>
/// Request to update usage statistics for a user.
/// Used for admin/migration purposes.
/// </summary>
public sealed record UpdateUsageStatsRequest(
    int? StudiesOwned,
    int? StudiesTotal,
    int? TracesThisPeriod,
    long? TracesTotal,
    int? MaxMembersInStudy,
    int? AlignmentsThisPeriod,
    long? AlignmentsTotal,
    long? AlignmentsCompleted,
    int? TracesPending);

/// <summary>
/// Request to increment a specific usage counter.
/// </summary>
public sealed record IncrementUsageRequest(
    string CounterType,
    int Amount = 1);
