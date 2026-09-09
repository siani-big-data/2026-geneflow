using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.PaymentMethods;

/// <summary>
/// Payment method aggregate root.
/// Stores reference to Stripe PaymentMethod.
/// </summary>
public sealed class PaymentMethod : AggregateRoot<PaymentMethodId>
{
    /// <summary>Gets the user ID.</summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>Gets the Stripe payment method ID.</summary>
    public string StripePaymentMethodId { get; private set; } = null!;

    /// <summary>Gets the card brand (visa, mastercard, etc.).</summary>
    public string Brand { get; private set; } = null!;

    /// <summary>Gets the last 4 digits of the card.</summary>
    public string Last4 { get; private set; } = null!;

    /// <summary>Gets the card expiration month.</summary>
    public int ExpiryMonth { get; private set; }

    /// <summary>Gets the card expiration year.</summary>
    public int ExpiryYear { get; private set; }

    /// <summary>Gets whether this is the default payment method.</summary>
    public bool IsDefault { get; private set; }

    /// <summary>Gets the creation date.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Gets the last modification date.</summary>
    public DateTime? ModifiedAt { get; private set; }

    private PaymentMethod()
    {
    }

    /// <summary>
    /// Creates a new payment method.
    /// </summary>
    public static Result<PaymentMethod> Create(
        PaymentMethodId id,
        UserId userId,
        string stripePaymentMethodId,
        string brand,
        string last4,
        int expiryMonth,
        int expiryYear,
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(stripePaymentMethodId))
            return Result.Failure<PaymentMethod>(PaymentMethodErrors.InvalidStripeId);

        if (string.IsNullOrWhiteSpace(last4) || last4.Length != 4)
            return Result.Failure<PaymentMethod>(PaymentMethodErrors.InvalidLast4);

        var paymentMethod = new PaymentMethod
        {
            Id = id,
            UserId = userId,
            StripePaymentMethodId = stripePaymentMethodId,
            Brand = brand.ToLowerInvariant(),
            Last4 = last4,
            ExpiryMonth = expiryMonth,
            ExpiryYear = expiryYear,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        };

        return paymentMethod;
    }

    /// <summary>
    /// Sets this payment method as the default.
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes the default status.
    /// </summary>
    public void RemoveDefault()
    {
        IsDefault = false;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the card details (from Stripe webhook).
    /// </summary>
    public void UpdateCardDetails(string brand, string last4, int expiryMonth, int expiryYear)
    {
        Brand = brand.ToLowerInvariant();
        Last4 = last4;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        ModifiedAt = DateTime.UtcNow;
    }
}
