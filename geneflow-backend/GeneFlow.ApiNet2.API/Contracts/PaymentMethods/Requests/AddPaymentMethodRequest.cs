namespace GeneFlow.ApiNet2.API.Contracts.PaymentMethods.Requests;

/// <summary>
/// Request to add a new payment method.
/// </summary>
public sealed record AddPaymentMethodRequest(
    string StripePaymentMethodId,
    bool SetAsDefault = true);
