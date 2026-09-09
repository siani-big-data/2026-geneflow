namespace GeneFlow.ApiNet2.SharedKernel.Domain.Validation;

/// <summary>
/// Interface for validators that validate business rules.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
public interface IValidator<in T>
{
    /// <summary>
    /// Validates the specified instance.
    /// </summary>
    ValidationResult Validate(T instance);

    /// <summary>
    /// Validates the specified instance asynchronously.
    /// Use for validations that require external resources (database, services, etc.).
    /// </summary>
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}
