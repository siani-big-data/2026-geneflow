using GeneFlow.ApiNet2.SharedKernel.Domain.Exceptions;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Validation;

/// <summary>
/// Exception thrown when validation fails.
/// Contains the full validation result with all errors.
/// </summary>
public sealed class ValidationException : DomainException
{
    /// <summary>
    /// Gets the validation result containing all errors.
    /// </summary>
    public ValidationResult ValidationResult { get; }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => ValidationResult.Errors;

    /// <summary>
    /// Initializes a new validation exception with a validation result.
    /// </summary>
    /// <param name="validationResult">The validation result containing errors.</param>
    public ValidationException(ValidationResult validationResult)
        : base(BuildMessage(validationResult))
    {
        ValidationResult = validationResult;
    }

    /// <summary>
    /// Initializes a new validation exception with a single error.
    /// </summary>
    /// <param name="propertyName">The property that failed validation.</param>
    /// <param name="message">The error message.</param>
    public ValidationException(string propertyName, string message)
        : this(ValidationResult.Failure(propertyName, message))
    {
    }

    private static string BuildMessage(ValidationResult result)
    {
        if (result.IsValid)
            return "Validation passed";

        var errorMessages = result.Errors.Select(e => $"- {e}");
        return $"Validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errorMessages)}";
    }
}
