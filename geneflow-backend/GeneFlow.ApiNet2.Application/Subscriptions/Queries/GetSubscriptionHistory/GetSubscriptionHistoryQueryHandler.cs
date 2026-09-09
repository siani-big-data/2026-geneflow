using GeneFlow.ApiNet2.Application.Subscriptions.DTOs;
using GeneFlow.ApiNet2.Application.Subscriptions.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Subscriptions.Queries.GetSubscriptionHistory;

/// <summary>
/// Handler for GetSubscriptionHistoryQuery.
/// </summary>
public sealed class GetSubscriptionHistoryQueryHandler
    : IQueryHandler<GetSubscriptionHistoryQuery, Result<IReadOnlyList<SubscriptionSummaryDto>>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GetSubscriptionHistoryQueryHandler(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionSummaryDto>>> Handle(
        GetSubscriptionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<IReadOnlyList<SubscriptionSummaryDto>>(SubscriptionErrors.NotFound);

        var subscriptions = await _subscriptionRepository.GetAllByUserIdAsync(userId, cancellationToken);
        return Result.Success<IReadOnlyList<SubscriptionSummaryDto>>(subscriptions.ToSummaryDtos());
    }
}
