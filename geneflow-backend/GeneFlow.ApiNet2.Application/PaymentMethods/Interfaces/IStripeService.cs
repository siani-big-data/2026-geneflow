namespace GeneFlow.ApiNet2.Application.PaymentMethods.Interfaces;

/// <summary>
/// Stripe integration service interface.
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Gets or creates a Stripe customer ID for a user.
    /// </summary>
    Task<string> GetOrCreateCustomerAsync(string userId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a SetupIntent for adding a new payment method.
    /// </summary>
    Task<string> CreateSetupIntentAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attaches a payment method to a customer.
    /// </summary>
    Task AttachPaymentMethodAsync(string paymentMethodId, string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detaches a payment method from a customer.
    /// </summary>
    Task DetachPaymentMethodAsync(string paymentMethodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the default payment method for a customer.
    /// </summary>
    Task SetDefaultPaymentMethodAsync(string customerId, string paymentMethodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payment method details from Stripe.
    /// </summary>
    Task<StripePaymentMethodDetails?> GetPaymentMethodDetailsAsync(string paymentMethodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns whether Stripe is configured.
    /// </summary>
    bool IsConfigured { get; }
}

/// <summary>
/// Payment method details from Stripe.
/// </summary>
public sealed record StripePaymentMethodDetails(
    string Id,
    string Brand,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear);
