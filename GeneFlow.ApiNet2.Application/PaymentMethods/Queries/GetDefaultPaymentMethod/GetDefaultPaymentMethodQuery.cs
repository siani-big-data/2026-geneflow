using GeneFlow.ApiNet2.Application.PaymentMethods.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.PaymentMethods.Queries.GetDefaultPaymentMethod;

/// <summary>
/// Query to get the default payment method for the current user.
/// </summary>
public sealed record GetDefaultPaymentMethodQuery(string UserId)
    : IQuery<Result<PaymentMethodDto?>>;
