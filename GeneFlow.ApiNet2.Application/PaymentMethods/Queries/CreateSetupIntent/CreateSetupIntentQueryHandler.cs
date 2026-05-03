using GeneFlow.ApiNet2.Application.PaymentMethods.DTOs;
using GeneFlow.ApiNet2.Application.PaymentMethods.Interfaces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.PaymentMethods.Queries.CreateSetupIntent;

/// <summary>
/// Handler for CreateSetupIntentQuery.
/// </summary>
public sealed class CreateSetupIntentQueryHandler
    : IQueryHandler<CreateSetupIntentQuery, Result<SetupIntentDto>>
{
    private readonly IStripeService _stripeService;

    public CreateSetupIntentQueryHandler(IStripeService stripeService)
    {
        _stripeService = stripeService;
    }

    public async Task<Result<SetupIntentDto>> Handle(
        CreateSetupIntentQuery request,
        CancellationToken cancellationToken)
    {
        // Get or create Stripe customer (stub returns fake ID in dev mode)
        var customerId = await _stripeService.GetOrCreateCustomerAsync(
            request.UserId,
            "", // Email will be fetched from user
            cancellationToken);

        // Create SetupIntent (stub returns fake secret in dev mode)
        var clientSecret = await _stripeService.CreateSetupIntentAsync(customerId, cancellationToken);

        return new SetupIntentDto(clientSecret);
    }
}
