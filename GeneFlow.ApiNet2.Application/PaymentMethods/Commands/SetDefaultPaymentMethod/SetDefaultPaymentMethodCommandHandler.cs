using GeneFlow.ApiNet2.Application.PaymentMethods.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.PaymentMethods;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.PaymentMethods.Commands.SetDefaultPaymentMethod;

/// <summary>
/// Handler for SetDefaultPaymentMethodCommand.
/// </summary>
public sealed class SetDefaultPaymentMethodCommandHandler
    : ICommandHandler<SetDefaultPaymentMethodCommand, Result>
{
    private readonly IPaymentMethodRepository _repository;
    private readonly IStripeService _stripeService;

    public SetDefaultPaymentMethodCommandHandler(
        IPaymentMethodRepository repository,
        IStripeService stripeService)
    {
        _repository = repository;
        _stripeService = stripeService;
    }

    public async Task<Result> Handle(
        SetDefaultPaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Parse(request.UserId);
        var paymentMethodId = PaymentMethodId.Parse(request.PaymentMethodId);

        var paymentMethod = await _repository.GetByIdAsync(paymentMethodId, cancellationToken);
        if (paymentMethod is null)
            return Result.Failure(PaymentMethodErrors.NotFound);

        // Verify ownership
        if (paymentMethod.UserId != userId)
            return Result.Failure(PaymentMethodErrors.NotFound);

        // Already default
        if (paymentMethod.IsDefault)
            return Result.Success();

        // Remove default from other methods
        var allMethods = await _repository.GetByUserIdAsync(userId, cancellationToken);
        foreach (var method in allMethods.Where(m => m.IsDefault))
        {
            method.RemoveDefault();
            await _repository.UpdateAsync(method, cancellationToken);
        }

        // Set new default
        paymentMethod.SetAsDefault();
        await _repository.UpdateAsync(paymentMethod, cancellationToken);

        // Update in Stripe if configured
        if (_stripeService.IsConfigured)
        {
            var customerId = await _stripeService.GetOrCreateCustomerAsync(
                request.UserId,
                "",
                cancellationToken);

            await _stripeService.SetDefaultPaymentMethodAsync(
                customerId,
                paymentMethod.StripePaymentMethodId,
                cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
