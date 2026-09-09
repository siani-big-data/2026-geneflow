namespace GeneFlow.ApiNet2.SharedKernel.Domain.Validation;

/// <summary>
/// Fluent builder for validation rules.
/// </summary>
public sealed class RuleBuilder<T, TProperty>
{
    private readonly Validator<T> _validator;
    private readonly ValidationRule<T, TProperty> _rule;

    internal RuleBuilder(Validator<T> validator, ValidationRule<T, TProperty> rule)
    {
        _validator = validator;
        _rule = rule;
    }

    /// <summary>
    /// Adds a custom validation condition.
    /// </summary>
    public RuleBuilder<T, TProperty> Must(Func<TProperty, bool> predicate, string message, string? errorCode = null)
    {
        _rule.AddCondition(
            (value, _) => predicate(value),
            (_, _) => message,
            errorCode);
        return this;
    }

    /// <summary>
    /// Adds a custom validation condition with access to the parent object.
    /// </summary>
    public RuleBuilder<T, TProperty> Must(Func<TProperty, T, bool> predicate, string message, string? errorCode = null)
    {
        _rule.AddCondition(predicate, (_, _) => message, errorCode);
        return this;
    }

    /// <summary>
    /// Adds a custom validation condition with dynamic message.
    /// </summary>
    public RuleBuilder<T, TProperty> Must(Func<TProperty, bool> predicate, Func<TProperty, string> messageBuilder, string? errorCode = null)
    {
        _rule.AddCondition(
            (value, _) => predicate(value),
            (value, _) => messageBuilder(value),
            errorCode);
        return this;
    }

    /// <summary>
    /// Value must not be null.
    /// </summary>
    public RuleBuilder<T, TProperty> NotNull(string? message = null, string? errorCode = null)
    {
        _rule.AddCondition(
            (value, _) => value is not null,
            (_, _) => message ?? "cannot be null",
            errorCode);
        return this;
    }

    /// <summary>
    /// Value must be equal to the specified value.
    /// </summary>
    public RuleBuilder<T, TProperty> Equal(TProperty expected, string? message = null, string? errorCode = null)
    {
        _rule.AddCondition(
            (value, _) => EqualityComparer<TProperty>.Default.Equals(value, expected),
            (_, _) => message ?? $"must be equal to '{expected}'",
            errorCode);
        return this;
    }

    /// <summary>
    /// Value must not be equal to the specified value.
    /// </summary>
    public RuleBuilder<T, TProperty> NotEqual(TProperty notExpected, string? message = null, string? errorCode = null)
    {
        _rule.AddCondition(
            (value, _) => !EqualityComparer<TProperty>.Default.Equals(value, notExpected),
            (_, _) => message ?? $"must not be equal to '{notExpected}'",
            errorCode);
        return this;
    }

    /// <summary>
    /// Only apply this rule when condition is true.
    /// </summary>
    public RuleBuilder<T, TProperty> When(Func<T, bool> condition)
    {
        _rule.WhenCondition = condition;
        return this;
    }

    /// <summary>
    /// Adds an async validation condition.
    /// </summary>
    public RuleBuilder<T, TProperty> MustAsync(
        Func<TProperty, CancellationToken, Task<bool>> predicate,
        string message,
        string? errorCode = null)
    {
        var asyncRule = new AsyncValidationRule<T, TProperty>(
            _rule.PropertySelector,
            _rule.PropertyName,
            (value, _, ct) => predicate(value, ct),
            (_, _) => message,
            errorCode)
        {
            WhenCondition = _rule.WhenCondition
        };

        _validator.AddAsyncRule(asyncRule);
        return this;
    }

    /// <summary>
    /// Adds an async validation condition with access to parent object.
    /// </summary>
    public RuleBuilder<T, TProperty> MustAsync(
        Func<TProperty, T, CancellationToken, Task<bool>> predicate,
        string message,
        string? errorCode = null)
    {
        var asyncRule = new AsyncValidationRule<T, TProperty>(
            _rule.PropertySelector,
            _rule.PropertyName,
            predicate,
            (_, _) => message,
            errorCode)
        {
            WhenCondition = _rule.WhenCondition
        };

        _validator.AddAsyncRule(asyncRule);
        return this;
    }
}
