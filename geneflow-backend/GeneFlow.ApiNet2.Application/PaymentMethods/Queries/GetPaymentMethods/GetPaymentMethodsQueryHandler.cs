using GeneFlow.ApiNet2.Application.PaymentMethods.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.PaymentMethods;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.PaymentMethods.Queries.GetPaymentMethods;

/// <summary>
/// Handler for GetPaymentMethodsQuery.
/// </summary>
public sealed class GetPaymentMethodsQueryHandler
    : IQueryHandler<GetPaymentMethodsQuery, Result<IReadOnlyList<PaymentMethodDto>>>
{
    private readonly IPaymentMethodRepository _repository;

    public GetPaymentMethodsQueryHandler(IPaymentMethodRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<PaymentMethodDto>>> Handle(
        GetPaymentMethodsQuery request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<IReadOnlyList<PaymentMethodDto>>(PaymentMethodErrors.InvalidUserId);

        var paymentMethods = await _repository.GetByUserIdAsync(userId, cancellationToken);

        var dtos = paymentMethods.Select(pm => new PaymentMethodDto(
            pm.Id.ToString(),
            pm.Brand,
            pm.Last4,
            pm.ExpiryMonth,
            pm.ExpiryYear,
            pm.IsDefault,
            pm.CreatedAt
        )).ToList();

        return Result.Success<IReadOnlyList<PaymentMethodDto>>(dtos);
    }
}
