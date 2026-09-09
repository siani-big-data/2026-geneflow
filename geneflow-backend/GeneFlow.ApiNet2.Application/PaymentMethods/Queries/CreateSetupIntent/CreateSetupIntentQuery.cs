using GeneFlow.ApiNet2.Application.PaymentMethods.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.PaymentMethods.Queries.CreateSetupIntent;

/// <summary>
/// Query to create a Stripe SetupIntent for adding a new payment method.
/// </summary>
public sealed record CreateSetupIntentQuery(string UserId)
    : IQuery<Result<SetupIntentDto>>;
