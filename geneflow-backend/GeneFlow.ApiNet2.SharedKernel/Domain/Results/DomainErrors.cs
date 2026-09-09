namespace GeneFlow.ApiNet2.SharedKernel.Domain.Results;

/// <summary>
/// Base class for defining domain-specific errors.
/// Inherit from this class to define errors for each domain/feature.
/// </summary>
/// <example>
/// public static class OrderErrors
/// {
///     public static Error NotFound(Guid id) => Error.NotFound(
///         "Order.NotFound", $"Order with ID '{id}' was not found");
///
///     public static Error AlreadyCancelled => Error.Conflict(
///         "Order.AlreadyCancelled", "Order has already been cancelled");
/// }
/// </example>
public static class DomainErrors
{
    /// <summary>
    /// General domain errors that apply across all contexts.
    /// </summary>
    public static class General
    {
        /// <summary>Creates a not found error for an entity.</summary>
        public static Error NotFound(string entityName, object id) => Error.NotFound(
            $"{entityName}.NotFound",
            $"{entityName} with ID '{id}' was not found");

        /// <summary>Creates a validation error for a required value.</summary>
        public static Error ValueIsRequired(string valueName) => Error.Validation(
            "General.ValueIsRequired",
            $"'{valueName}' is required");

        /// <summary>Creates a validation error for an invalid value.</summary>
        public static Error InvalidValue(string valueName) => Error.Validation(
            "General.InvalidValue",
            $"'{valueName}' has an invalid value");

        /// <summary>Creates an unexpected error.</summary>
        public static Error UnexpectedError(string message) => Error.Unexpected(
            "General.Unexpected",
            message);

        /// <summary>Creates a conflict error.</summary>
        public static Error Conflict(string message) => Error.Conflict(
            "General.Conflict",
            message);
    }
}
