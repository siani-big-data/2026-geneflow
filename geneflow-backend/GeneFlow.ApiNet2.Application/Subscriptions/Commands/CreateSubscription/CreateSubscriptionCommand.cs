using GeneFlow.ApiNet2.Application.Subscriptions.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Subscriptions.Commands.CreateSubscription;

/// <summary>
/// Command to create a new subscription.
/// </summary>
public sealed record CreateSubscriptionCommand(
    string UserId,
    string PlanId,
    int BillingCycleId,
    bool StartWithTrial = false) : ICommand<Result<SubscriptionDto>>;
