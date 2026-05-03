using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.PaymentMethods.Commands.RemovePaymentMethod;

/// <summary>
/// Command to remove a payment method.
/// </summary>
public sealed record RemovePaymentMethodCommand(
    string UserId,
    string PaymentMethodId)
    : ICommand<Result>;
