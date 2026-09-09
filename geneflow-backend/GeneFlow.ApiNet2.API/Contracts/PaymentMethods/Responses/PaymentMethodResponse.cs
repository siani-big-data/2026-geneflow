namespace GeneFlow.ApiNet2.API.Contracts.PaymentMethods.Responses;

/// <summary>
/// Payment method response.
/// </summary>
public sealed record PaymentMethodResponse(
    string Id,
    string Brand,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    bool IsDefault,
    DateTime CreatedAt);

/// <summary>
/// Setup intent response for adding a new payment method.
/// </summary>
public sealed record SetupIntentResponse(
    string ClientSecret);
