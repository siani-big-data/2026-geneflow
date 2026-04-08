namespace GeneFlow.ApiNet2.API.Contracts.Subscriptions.Requests;

/// <summary>
/// Request to change subscription plan.
/// </summary>
public sealed record ChangePlanRequest(
    Guid NewPlanId,
    int BillingCycleId);
