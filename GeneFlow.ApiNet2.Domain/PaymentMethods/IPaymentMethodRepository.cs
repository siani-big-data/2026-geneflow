using GeneFlow.ApiNet2.Domain.Identity;

namespace GeneFlow.ApiNet2.Domain.PaymentMethods;

/// <summary>
/// Repository interface for payment methods.
/// </summary>
public interface IPaymentMethodRepository
{
    /// <summary>
    /// Gets a payment method by ID.
    /// </summary>
    Task<PaymentMethod?> GetByIdAsync(PaymentMethodId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payment methods for a user.
    /// </summary>
    Task<IReadOnlyList<PaymentMethod>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default payment method for a user.
    /// </summary>
    Task<PaymentMethod?> GetDefaultByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payment method by Stripe ID.
    /// </summary>
    Task<PaymentMethod?> GetByStripeIdAsync(string stripePaymentMethodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new payment method.
    /// </summary>
    Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a payment method.
    /// </summary>
    Task UpdateAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a payment method.
    /// </summary>
    Task RemoveAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
