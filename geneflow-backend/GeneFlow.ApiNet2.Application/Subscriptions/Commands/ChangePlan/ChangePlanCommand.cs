using GeneFlow.ApiNet2.Application.Subscriptions.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Subscriptions.Commands.ChangePlan;

/// <summary>
/// Command to change subscription plan.
/// </summary>
public sealed record ChangePlanCommand(
    string UserId,
    string NewPlanId,
    int BillingCycleId) : ICommand<Result<SubscriptionDto>>;
