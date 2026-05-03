using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.PaymentMethods;

/// <summary>
/// Payment method domain errors.
/// </summary>
public static class PaymentMethodErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "PaymentMethod.NotFound",
        "Payment method not found.");

    public static readonly Error InvalidStripeId = Error.Validation(
        "PaymentMethod.InvalidStripeId",
        "Invalid Stripe payment method ID.");

    public static readonly Error InvalidLast4 = Error.Validation(
        "PaymentMethod.InvalidLast4",
        "Invalid card last 4 digits.");

    public static readonly Error AlreadyExists = Error.Conflict(
        "PaymentMethod.AlreadyExists",
        "This payment method is already registered.");

    public static readonly Error CannotRemoveDefault = Error.Validation(
        "PaymentMethod.CannotRemoveDefault",
        "Cannot remove the default payment method. Set another payment method as default first.");

    public static readonly Error StripeNotConfigured = Error.Failure(
        "PaymentMethod.StripeNotConfigured",
        "Stripe is not configured. Please contact support.");
}
