using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Subscriptions.Commands.CancelSubscription;

/// <summary>
/// Handler for CancelSubscriptionCommand.
/// </summary>
public sealed class CancelSubscriptionCommandHandler
    : ICommandHandler<CancelSubscriptionCommand, Result>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public CancelSubscriptionCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(
        CancelSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(SubscriptionErrors.NotFound);

        // Get active subscription
        var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
            return Result.Failure(SubscriptionErrors.NotFound);

        // Cancel subscription
        var cancelResult = subscription.Cancel(request.Reason);
        if (cancelResult.IsFailure)
            return cancelResult;

        _subscriptionRepository.Update(subscription);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
