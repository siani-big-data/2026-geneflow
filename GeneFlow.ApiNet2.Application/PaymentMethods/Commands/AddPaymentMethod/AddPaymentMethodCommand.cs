using GeneFlow.ApiNet2.Application.PaymentMethods.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.PaymentMethods.Commands.AddPaymentMethod;

/// <summary>
/// Command to add a new payment method after Stripe SetupIntent confirmation.
/// </summary>
public sealed record AddPaymentMethodCommand(
    string UserId,
    string StripePaymentMethodId,
    bool SetAsDefault = true)
    : ICommand<Result<PaymentMethodDto>>;
