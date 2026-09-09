using System.Linq.Expressions;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Validation;

/// <summary>
/// Base class for validators with fluent rule configuration.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
public abstract class Validator<T> : IValidator<T>
{
    private readonly List<object> _rules = [];
    private readonly List<object> _asyncRules = [];

    /// <summary>
    /// Defines a validation rule for a property.
    /// </summary>
    protected RuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
    {
        var propertyName = GetPropertyName(propertyExpression);
        var propertySelector = propertyExpression.Compile();

        var rule = new ValidationRule<T, TProperty>(propertySelector, propertyName);
        _rules.Add(rule);

        return new RuleBuilder<T, TProperty>(this, rule);
    }

    /// <summary>
    /// Adds a general rule that applies to the entire object.
    /// </summary>
    protected void RuleFor(Func<T, bool> condition, string propertyName, string message, string? errorCode = null)
    {
        var rule = new ValidationRule<T, T>(x => x, propertyName);
        rule.AddCondition((_, instance) => condition(instance), (_, _) => message, errorCode);
        _rules.Add(rule);
    }

    internal void AddAsyncRule<TProperty>(AsyncValidationRule<T, TProperty> rule)
    {
        _asyncRules.Add(rule);
    }

    /// <inheritdoc />
    public ValidationResult Validate(T instance)
    {
        var errors = new List<ValidationError>();

        foreach (var rule in _rules)
        {
            var method = rule.GetType().GetMethod("Evaluate");
            if (method?.Invoke(rule, [instance]) is IEnumerable<ValidationError> ruleErrors)
            {
                errors.AddRange(ruleErrors);
            }
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        // Run sync rules first
        foreach (var rule in _rules)
        {
            var method = rule.GetType().GetMethod("Evaluate");
            if (method?.Invoke(rule, [instance]) is IEnumerable<ValidationError> ruleErrors)
            {
                errors.AddRange(ruleErrors);
            }
        }

        // Run async rules
        foreach (var rule in _asyncRules)
        {
            var method = rule.GetType().GetMethod("EvaluateAsync");
            if (method is not null)
            {
                var task = method.Invoke(rule, [instance, cancellationToken]) as Task<ValidationError?>;
                if (task is not null)
                {
                    var error = await task;
                    if (error is not null)
                    {
                        errors.Add(error);
                    }
                }
            }
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }

    private static string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression { Operand: MemberExpression operand })
        {
            return operand.Member.Name;
        }

        return "Unknown";
    }
}
