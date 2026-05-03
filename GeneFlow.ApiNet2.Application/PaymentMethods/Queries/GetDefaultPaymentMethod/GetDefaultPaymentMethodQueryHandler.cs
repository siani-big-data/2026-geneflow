using GeneFlow.ApiNet2.Application.PaymentMethods.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.PaymentMethods;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.PaymentMethods.Queries.GetDefaultPaymentMethod;

/// <summary>
/// Handler for GetDefaultPaymentMethodQuery.
/// </summary>
public sealed class GetDefaultPaymentMethodQueryHandler
    : IQueryHandler<GetDefaultPaymentMethodQuery, Result<PaymentMethodDto?>>
{
    private readonly IPaymentMethodRepository _repository;

    public GetDefaultPaymentMethodQueryHandler(IPaymentMethodRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PaymentMethodDto?>> Handle(
        GetDefaultPaymentMethodQuery request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Parse(request.UserId);
        var paymentMethod = await _repository.GetDefaultByUserIdAsync(userId, cancellationToken);

        if (paymentMethod is null)
            return Result.Success<PaymentMethodDto?>(null);

        var dto = new PaymentMethodDto(
            paymentMethod.Id.ToString(),
            paymentMethod.Brand,
            paymentMethod.Last4,
            paymentMethod.ExpiryMonth,
            paymentMethod.ExpiryYear,
            paymentMethod.IsDefault,
            paymentMethod.CreatedAt
        );

        return Result.Success<PaymentMethodDto?>(dto);
    }
}
