namespace GeneFlow.ApiNet2.API.Contracts.Subscriptions.Requests;

/// <summary>
/// Request to create a new subscription.
/// </summary>
public sealed record CreateSubscriptionRequest(
    Guid PlanId,
    int BillingCycleId,
    bool StartWithTrial = false);
