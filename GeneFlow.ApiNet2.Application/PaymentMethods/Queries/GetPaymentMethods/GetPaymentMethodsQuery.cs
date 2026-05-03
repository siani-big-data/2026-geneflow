using GeneFlow.ApiNet2.Application.PaymentMethods.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.PaymentMethods.Queries.GetPaymentMethods;

/// <summary>
/// Query to get all payment methods for the current user.
/// </summary>
public sealed record GetPaymentMethodsQuery(string UserId)
    : IQuery<Result<IReadOnlyList<PaymentMethodDto>>>;
