namespace GeneFlow.ApiNet2.API.Contracts.Subscriptions.Requests;

/// <summary>
/// Request to cancel a subscription.
/// </summary>
public sealed record CancelSubscriptionRequest(string? Reason = null);
