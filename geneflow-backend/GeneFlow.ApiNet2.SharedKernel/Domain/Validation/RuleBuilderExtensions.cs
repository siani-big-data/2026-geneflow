namespace GeneFlow.ApiNet2.SharedKernel.Domain.Validation;

/// <summary>
/// Extension methods for RuleBuilder to add common validation rules.
/// </summary>
public static class RuleBuilderExtensions
{
    /// <summary>
    /// String must not be null or empty.
    /// </summary>
    public static RuleBuilder<T, string?> NotEmpty<T>(
        this RuleBuilder<T, string?> builder,
        string? message = null,
        string? errorCode = null)
    {
        return builder.Must(
            value => !string.IsNullOrEmpty(value),
            message ?? "cannot be empty",
            errorCode);
    }

    /// <summary>
    /// String must not be null, empty, or whitespace.
    /// </summary>
    public static RuleBuilder<T, string?> NotWhiteSpace<T>(
        this RuleBuilder<T, string?> builder,
        string? message = null,
        string? errorCode = null)
    {
        return builder.Must(
            value => !string.IsNullOrWhiteSpace(value),
            message ?? "cannot be empty or whitespace",
            errorCode);
    }

    /// <summary>
    /// String length must not exceed maximum.
    /// </summary>
    public static RuleBuilder<T, string?> MaxLength<T>(
        this RuleBuilder<T, string?> builder,
        int maxLength,
        string? message = null,
        string? errorCode = null)
    {
        return builder.Must(
            value => value is null || value.Length <= maxLength,
            value => message ?? $"must not exceed {maxLength} characters (was {value?.Length ?? 0})",
            errorCode);
    }

    /// <summary>
    /// String length must be at least minimum.
    /// </summary>
    public static RuleBuilder<T, string?> MinLength<T>(
        this RuleBuilder<T, string?> builder,
        int minLength,
        string? message = null,
        string? errorCode = null)
    {
        return builder.Must(
            value => value is not null && value.Length >= minLength,
            value => message ?? $"must be at least {minLength} characters (was {value?.Length ?? 0})",
            errorCode);
    }

    /// <summary>
    /// String must match the specified pattern.
    /// </summary>
    public static RuleBuilder<T, string?> Matches<T>(
        this RuleBuilder<T, string?> builder,
        string pattern,
        string? message = null,
        string? errorCode = null)
    {
        var regex = new System.Text.RegularExpressions.Regex(pattern);
        return builder.Must(
            value => value is not null && regex.IsMatch(value),
            message ?? $"must match pattern '{pattern}'",
            errorCode);
    }

    /// <summary>
    /// String must be a valid email address.
    /// </summary>
    public static RuleBuilder<T, string?> EmailAddress<T>(
        this RuleBuilder<T, string?> builder,
        string? message = null,
        string? errorCode = null)
    {
        const string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return builder.Matches(emailPattern, message ?? "must be a valid email address", errorCode);
    }

    /// <summary>
    /// Number must be greater than the specified value.
    /// </summary>
    public static RuleBuilder<T, TProperty> GreaterThan<T, TProperty>(
        this RuleBuilder<T, TProperty> builder,
        TProperty threshold,
        string? message = null,
        string? errorCode = null)
        where TProperty : IComparable<TProperty>
    {
        return builder.Must(
            value => value.CompareTo(threshold) > 0,
            message ?? $"must be greater than {threshold}",
            errorCode);
    }

    /// <summary>
    /// Number must be greater than or equal to the specified value.
    /// </summary>
    public static RuleBuilder<T, TProperty> GreaterThanOrEqual<T, TProperty>(
        this RuleBuilder<T, TProperty> builder,
        TProperty threshold,
        string? message = null,
        string? errorCode = null)
        where TProperty : IComparable<TProperty>
    {
        return builder.Must(
            value => value.CompareTo(threshold) >= 0,
            message ?? $"must be greater than or equal to {threshold}",
            errorCode);
    }

    /// <summary>
    /// Number must be less than the specified value.
    /// </summary>
    public static RuleBuilder<T, TProperty> LessThan<T, TProperty>(
        this RuleBuilder<T, TProperty> builder,
        TProperty threshold,
        string? message = null,
        string? errorCode = null)
        where TProperty : IComparable<TProperty>
    {
        return builder.Must(
            value => value.CompareTo(threshold) < 0,
            message ?? $"must be less than {threshold}",
            errorCode);
    }

    /// <summary>
    /// Number must be less than or equal to the specified value.
    /// </summary>
    public static RuleBuilder<T, TProperty> LessThanOrEqual<T, TProperty>(
        this RuleBuilder<T, TProperty> builder,
        TProperty threshold,
        string? message = null,
        string? errorCode = null)
        where TProperty : IComparable<TProperty>
    {
        return builder.Must(
            value => value.CompareTo(threshold) <= 0,
            message ?? $"must be less than or equal to {threshold}",
            errorCode);
    }

    /// <summary>
    /// Number must be within the specified range (inclusive).
    /// </summary>
    public static RuleBuilder<T, TProperty> InRange<T, TProperty>(
        this RuleBuilder<T, TProperty> builder,
        TProperty min,
        TProperty max,
        string? message = null,
        string? errorCode = null)
        where TProperty : IComparable<TProperty>
    {
        return builder.Must(
            value => value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0,
            message ?? $"must be between {min} and {max}",
            errorCode);
    }


    /// <summary>
    /// Collection must not be null or empty.
    /// </summary>
    public static RuleBuilder<T, IEnumerable<TElement>?> NotEmpty<T, TElement>(
        this RuleBuilder<T, IEnumerable<TElement>?> builder,
        string? message = null,
        string? errorCode = null)
    {
        return builder.Must(
            value => value is not null && value.Any(),
            message ?? "cannot be empty",
            errorCode);
    }

    /// <summary>
    /// Collection count must not exceed maximum.
    /// </summary>
    public static RuleBuilder<T, IEnumerable<TElement>?> MaxCount<T, TElement>(
        this RuleBuilder<T, IEnumerable<TElement>?> builder,
        int maxCount,
        string? message = null,
        string? errorCode = null)
    {
        return builder.Must(
            value => value is null || value.Count() <= maxCount,
            message ?? $"must not contain more than {maxCount} items",
            errorCode);
    }


    /// <summary>
    /// Guid must not be empty.
    /// </summary>
    public static RuleBuilder<T, Guid> NotEmpty<T>(
        this RuleBuilder<T, Guid> builder,
        string? message = null,
        string? errorCode = null)
    {
        return builder.Must(
            value => value != Guid.Empty,
            message ?? "cannot be empty",
            errorCode);
    }
}
