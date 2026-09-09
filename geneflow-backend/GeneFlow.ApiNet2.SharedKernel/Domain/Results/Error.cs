namespace GeneFlow.ApiNet2.SharedKernel.Domain.Results;

/// <summary>
/// Represents an error with a code and message.
/// </summary>
public sealed record Error
{
    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Error type for categorization.
    /// </summary>
    public ErrorType Type { get; }

    private Error(string code, string message, ErrorType type)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    /// <summary>
    /// Creates a failure error (general business rule violation).
    /// </summary>
    public static Error Failure(string code, string message)
        => new(code, message, ErrorType.Failure);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string code, string message)
        => new(code, message, ErrorType.Validation);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    public static Error NotFound(string code, string message)
        => new(code, message, ErrorType.NotFound);

    /// <summary>
    /// Creates a conflict error (duplicate, already exists, etc.).
    /// </summary>
    public static Error Conflict(string code, string message)
        => new(code, message, ErrorType.Conflict);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    public static Error Unauthorized(string code, string message)
        => new(code, message, ErrorType.Unauthorized);

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    public static Error Forbidden(string code, string message)
        => new(code, message, ErrorType.Forbidden);

    /// <summary>
    /// Creates an unexpected error.
    /// </summary>
    public static Error Unexpected(string code, string message)
        => new(code, message, ErrorType.Unexpected);

    /// <summary>
    /// No error (represents success).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    /// <inheritdoc />
    public override string ToString() => $"[{Code}] {Message}";
}

/// <summary>
/// Categorizes the type of error for proper handling.
/// </summary>
public enum ErrorType
{
    /// <summary>No error (success).</summary>
    None = 0,

    /// <summary>General business rule failure.</summary>
    Failure = 1,

    /// <summary>Input validation error.</summary>
    Validation = 2,

    /// <summary>Requested resource not found.</summary>
    NotFound = 3,

    /// <summary>Conflict with existing resource state.</summary>
    Conflict = 4,

    /// <summary>Authentication required.</summary>
    Unauthorized = 5,

    /// <summary>Insufficient permissions.</summary>
    Forbidden = 6,

    /// <summary>Unexpected system error.</summary>
    Unexpected = 7
}
