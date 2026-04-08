using GeneFlow.ApiNet2.Application.Subscriptions.DTOs;
using GeneFlow.ApiNet2.Application.Subscriptions.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Subscriptions.Queries.GetCurrentSubscription;

/// <summary>
/// Handler for GetCurrentSubscriptionQuery.
/// </summary>
public sealed class GetCurrentSubscriptionQueryHandler
    : IQueryHandler<GetCurrentSubscriptionQuery, Result<SubscriptionDto>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GetCurrentSubscriptionQueryHandler(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    /// <inheritdoc />
    public async Task<Result<SubscriptionDto>> Handle(
        GetCurrentSubscriptionQuery request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.NotFound);

        var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.NotFound);

        return subscription.ToDto();
    }
}
