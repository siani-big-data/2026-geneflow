using GeneFlow.ApiNet2.Application.PaymentMethods.DTOs;
using GeneFlow.ApiNet2.Application.PaymentMethods.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.PaymentMethods;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.PaymentMethods.Commands.AddPaymentMethod;

/// <summary>
/// Handler for AddPaymentMethodCommand.
/// </summary>
public sealed class AddPaymentMethodCommandHandler
    : ICommandHandler<AddPaymentMethodCommand, Result<PaymentMethodDto>>
{
    private readonly IPaymentMethodRepository _repository;
    private readonly IStripeService _stripeService;
    private readonly ISequenceGenerator _sequenceGenerator;

    public AddPaymentMethodCommandHandler(
        IPaymentMethodRepository repository,
        IStripeService stripeService,
        ISequenceGenerator sequenceGenerator)
    {
        _repository = repository;
        _stripeService = stripeService;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<Result<PaymentMethodDto>> Handle(
        AddPaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Parse(request.UserId);

        // Check if already exists
        var existing = await _repository.GetByStripeIdAsync(request.StripePaymentMethodId, cancellationToken);
        if (existing is not null)
            return Result.Failure<PaymentMethodDto>(PaymentMethodErrors.AlreadyExists);

        // Get payment method details from Stripe
        var details = await _stripeService.GetPaymentMethodDetailsAsync(
            request.StripePaymentMethodId,
            cancellationToken);

        if (details is null)
            return Result.Failure<PaymentMethodDto>(PaymentMethodErrors.InvalidStripeId);

        // If setting as default, remove default from existing methods
        if (request.SetAsDefault)
        {
            var existingMethods = await _repository.GetByUserIdAsync(userId, cancellationToken);
            foreach (var method in existingMethods.Where(m => m.IsDefault))
            {
                method.RemoveDefault();
                await _repository.UpdateAsync(method, cancellationToken);
            }
        }

        // Generate payment method ID
        var sequenceId = await _sequenceGenerator.NextAsync(PaymentMethodId.SequenceName, cancellationToken);
        var paymentMethodId = PaymentMethodId.FromSequence(sequenceId);

        // Create payment method
        var paymentMethodResult = PaymentMethod.Create(
            paymentMethodId,
            userId,
            request.StripePaymentMethodId,
            details.Brand,
            details.Last4,
            details.ExpiryMonth,
            details.ExpiryYear,
            request.SetAsDefault);

        if (paymentMethodResult.IsFailure)
            return Result.Failure<PaymentMethodDto>(paymentMethodResult.Error);

        var paymentMethod = paymentMethodResult.Value;
        await _repository.AddAsync(paymentMethod, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new PaymentMethodDto(
            paymentMethod.Id.ToString(),
            paymentMethod.Brand,
            paymentMethod.Last4,
            paymentMethod.ExpiryMonth,
            paymentMethod.ExpiryYear,
            paymentMethod.IsDefault,
            paymentMethod.CreatedAt);
    }
}
