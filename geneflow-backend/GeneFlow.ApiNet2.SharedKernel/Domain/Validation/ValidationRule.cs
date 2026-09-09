namespace GeneFlow.ApiNet2.SharedKernel.Domain.Validation;

/// <summary>
/// Represents a validation rule for a property.
/// </summary>
internal sealed class ValidationRule<T, TProperty>
{
    public Func<T, TProperty> PropertySelector { get; }
    public string PropertyName { get; }
    public List<Func<TProperty, T, bool>> Conditions { get; } = [];
    public List<Func<TProperty, T, string>> MessageBuilders { get; } = [];
    public List<string?> ErrorCodes { get; } = [];
    public Func<T, bool>? WhenCondition { get; set; }

    public ValidationRule(Func<T, TProperty> propertySelector, string propertyName)
    {
        PropertySelector = propertySelector;
        PropertyName = propertyName;
    }

    public void AddCondition(Func<TProperty, T, bool> condition, Func<TProperty, T, string> messageBuilder, string? errorCode = null)
    {
        Conditions.Add(condition);
        MessageBuilders.Add(messageBuilder);
        ErrorCodes.Add(errorCode);
    }

    public IEnumerable<ValidationError> Evaluate(T instance)
    {
        if (WhenCondition is not null && !WhenCondition(instance))
            yield break;

        var value = PropertySelector(instance);

        for (int i = 0; i < Conditions.Count; i++)
        {
            if (!Conditions[i](value, instance))
            {
                yield return new ValidationError(
                    PropertyName,
                    MessageBuilders[i](value, instance),
                    ErrorCodes[i]);
            }
        }
    }
}

/// <summary>
/// Represents an async validation rule.
/// </summary>
internal sealed class AsyncValidationRule<T, TProperty>
{
    public Func<T, TProperty> PropertySelector { get; }
    public string PropertyName { get; }
    public Func<TProperty, T, CancellationToken, Task<bool>> Condition { get; }
    public Func<TProperty, T, string> MessageBuilder { get; }
    public string? ErrorCode { get; }
    public Func<T, bool>? WhenCondition { get; set; }

    public AsyncValidationRule(
        Func<T, TProperty> propertySelector,
        string propertyName,
        Func<TProperty, T, CancellationToken, Task<bool>> condition,
        Func<TProperty, T, string> messageBuilder,
        string? errorCode = null)
    {
        PropertySelector = propertySelector;
        PropertyName = propertyName;
        Condition = condition;
        MessageBuilder = messageBuilder;
        ErrorCode = errorCode;
    }

    public async Task<ValidationError?> EvaluateAsync(T instance, CancellationToken cancellationToken)
    {
        if (WhenCondition is not null && !WhenCondition(instance))
            return null;

        var value = PropertySelector(instance);

        if (!await Condition(value, instance, cancellationToken))
        {
            return new ValidationError(PropertyName, MessageBuilder(value, instance), ErrorCode);
        }

        return null;
    }
}
