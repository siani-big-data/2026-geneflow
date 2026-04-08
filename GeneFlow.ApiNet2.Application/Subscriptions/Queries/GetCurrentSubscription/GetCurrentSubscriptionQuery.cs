using GeneFlow.ApiNet2.Application.Subscriptions.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Subscriptions.Queries.GetCurrentSubscription;

/// <summary>
/// Query to get the current user's active subscription.
/// </summary>
public sealed record GetCurrentSubscriptionQuery(string UserId) : IQuery<Result<SubscriptionDto>>;
