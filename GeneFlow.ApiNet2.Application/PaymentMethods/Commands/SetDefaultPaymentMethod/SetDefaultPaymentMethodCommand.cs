using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.PaymentMethods.Commands.SetDefaultPaymentMethod;

/// <summary>
/// Command to set a payment method as default.
/// </summary>
public sealed record SetDefaultPaymentMethodCommand(
    string UserId,
    string PaymentMethodId)
    : ICommand<Result>;
