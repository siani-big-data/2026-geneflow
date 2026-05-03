namespace GeneFlow.ApiNet2.Application.PaymentMethods.DTOs;

/// <summary>
/// Payment method data transfer object.
/// </summary>
public sealed record PaymentMethodDto(
    string Id,
    string Brand,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    bool IsDefault,
    DateTime CreatedAt);

/// <summary>
/// Setup intent for adding a new payment method.
/// </summary>
public sealed record SetupIntentDto(
    string ClientSecret);
