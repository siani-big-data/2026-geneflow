using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Subscriptions.Commands.CancelSubscription;

/// <summary>
/// Command to cancel a subscription.
/// </summary>
public sealed record CancelSubscriptionCommand(
    string UserId,
    string? Reason = null) : ICommand<Result>;
