using GeneFlow.ApiNet2.Application.PaymentMethods.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.PaymentMethods;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.PaymentMethods.Commands.RemovePaymentMethod;

/// <summary>
/// Handler for RemovePaymentMethodCommand.
/// </summary>
public sealed class RemovePaymentMethodCommandHandler
    : ICommandHandler<RemovePaymentMethodCommand, Result>
{
    private readonly IPaymentMethodRepository _repository;
    private readonly IStripeService _stripeService;

    public RemovePaymentMethodCommandHandler(
        IPaymentMethodRepository repository,
        IStripeService stripeService)
    {
        _repository = repository;
        _stripeService = stripeService;
    }

    public async Task<Result> Handle(
        RemovePaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(PaymentMethodErrors.InvalidUserId);

        if (!PaymentMethodId.TryParse(request.PaymentMethodId, out var paymentMethodId) || paymentMethodId is null)
            return Result.Failure(PaymentMethodErrors.NotFound);

        var paymentMethod = await _repository.GetByIdAsync(paymentMethodId, cancellationToken);
        if (paymentMethod is null)
            return Result.Failure(PaymentMethodErrors.NotFound);

        // Verify ownership
        if (paymentMethod.UserId != userId)
            return Result.Failure(PaymentMethodErrors.NotFound);

        // Check if it's the default and there are other methods
        if (paymentMethod.IsDefault)
        {
            var otherMethods = await _repository.GetByUserIdAsync(userId, cancellationToken);
            if (otherMethods.Count > 1)
                return Result.Failure(PaymentMethodErrors.CannotRemoveDefault);
        }

        // Detach from Stripe if configured
        if (_stripeService.IsConfigured)
        {
            await _stripeService.DetachPaymentMethodAsync(
                paymentMethod.StripePaymentMethodId,
                cancellationToken);
        }

        await _repository.RemoveAsync(paymentMethod, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
