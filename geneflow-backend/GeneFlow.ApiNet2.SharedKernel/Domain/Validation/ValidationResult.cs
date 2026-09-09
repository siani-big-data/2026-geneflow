namespace GeneFlow.ApiNet2.SharedKernel.Domain.Validation;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed class ValidationResult
{
    private readonly List<ValidationError> _errors;

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Gets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Gets a value indicating whether the validation failed.
    /// </summary>
    public bool IsInvalid => _errors.Count > 0;

    private ValidationResult(List<ValidationError> errors)
    {
        _errors = errors;
    }

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    public static ValidationResult Success() => new([]);

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) => new([.. errors]);

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    public static ValidationResult Failure(string propertyName, string message, string? code = null)
        => new([new ValidationError(propertyName, message, code)]);

    /// <summary>
    /// Combines multiple validation results into one.
    /// </summary>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var allErrors = results.SelectMany(r => r.Errors).ToList();
        return new ValidationResult(allErrors);
    }

    /// <summary>
    /// Gets errors for a specific property.
    /// </summary>
    public IEnumerable<ValidationError> GetErrorsFor(string propertyName)
        => _errors.Where(e => e.PropertyName == propertyName);

    /// <summary>
    /// Throws a ValidationException if the result is invalid.
    /// </summary>
    public void ThrowIfInvalid()
    {
        if (IsInvalid)
            throw new ValidationException(this);
    }

    /// <inheritdoc />
    public override string ToString()
        => IsValid ? "Valid" : string.Join("; ", _errors.Select(e => e.ToString()));
}
