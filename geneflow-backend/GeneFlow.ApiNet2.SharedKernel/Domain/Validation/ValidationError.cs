namespace GeneFlow.ApiNet2.SharedKernel.Domain.Validation;

/// <summary>
/// Represents a single validation error.
/// </summary>
public sealed record ValidationError
{
    /// <summary>
    /// The property or field that failed validation.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// The error message describing the validation failure.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Optional error code for programmatic error handling.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Initializes a new validation error.
    /// </summary>
    /// <param name="propertyName">The property that failed validation.</param>
    /// <param name="message">The error message.</param>
    /// <param name="code">Optional error code.</param>
    public ValidationError(string propertyName, string message, string? code = null)
    {
        PropertyName = propertyName;
        Message = message;
        Code = code;
    }

    /// <inheritdoc />
    public override string ToString() => $"{PropertyName}: {Message}";
}
