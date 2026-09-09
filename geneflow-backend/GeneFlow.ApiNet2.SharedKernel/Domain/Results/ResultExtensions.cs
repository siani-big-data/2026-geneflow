namespace GeneFlow.ApiNet2.SharedKernel.Domain.Results;

/// <summary>
/// Extension methods for Result types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps the value of an async result.
    /// </summary>
    public static async Task<Result<TNew>> Map<TValue, TNew>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, TNew> mapper)
    {
        var result = await resultTask;
        return result.Map(mapper);
    }

    /// <summary>
    /// Binds an async result to another operation.
    /// </summary>
    public static async Task<Result<TNew>> Bind<TValue, TNew>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, Result<TNew>> binder)
    {
        var result = await resultTask;
        return result.Bind(binder);
    }

    /// <summary>
    /// Binds an async result to another async operation.
    /// </summary>
    public static async Task<Result<TNew>> Bind<TValue, TNew>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, Task<Result<TNew>>> binder)
    {
        var result = await resultTask;
        return await result.BindAsync(binder);
    }

    /// <summary>
    /// Matches an async result.
    /// </summary>
    public static async Task<TResult> Match<TValue, TResult>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, TResult> onSuccess,
        Func<Error[], TResult> onFailure)
    {
        var result = await resultTask;
        return result.Match(onSuccess, onFailure);
    }

    /// <summary>
    /// Ensures a condition on an async result.
    /// </summary>
    public static async Task<Result<TValue>> Ensure<TValue>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, bool> predicate,
        Error error)
    {
        var result = await resultTask;
        return result.Ensure(predicate, error);
    }

    /// <summary>
    /// Executes an action on success for an async result.
    /// </summary>
    public static async Task<Result<TValue>> OnSuccess<TValue>(
        this Task<Result<TValue>> resultTask,
        Action<TValue> action)
    {
        var result = await resultTask;
        return result.OnSuccess(action);
    }

    /// <summary>
    /// Executes an async action on success.
    /// </summary>
    public static async Task<Result<TValue>> OnSuccessAsync<TValue>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, Task> action)
    {
        var result = await resultTask;
        if (result.IsSuccess)
            await action(result.Value);
        return result;
    }

    /// <summary>
    /// Executes an action on failure for an async result.
    /// </summary>
    public static async Task<Result<TValue>> OnFailure<TValue>(
        this Task<Result<TValue>> resultTask,
        Action<Error[]> action)
    {
        var result = await resultTask;
        return result.OnFailure(action);
    }

    /// <summary>
    /// Converts a ValidationResult to a Result.
    /// </summary>
    public static Result ToResult(this Validation.ValidationResult validationResult)
    {
        if (validationResult.IsValid)
            return Result.Success();

        var errors = validationResult.Errors
            .Select(e => Error.Validation(e.Code ?? "VALIDATION_ERROR", e.Message))
            .ToArray();

        return Result.Failure(errors);
    }

    /// <summary>
    /// Converts a ValidationResult to a Result with value.
    /// </summary>
    public static Result<TValue> ToResult<TValue>(
        this Validation.ValidationResult validationResult,
        TValue value)
    {
        if (validationResult.IsValid)
            return Result<TValue>.Success(value);

        var errors = validationResult.Errors
            .Select(e => Error.Validation(e.Code ?? "VALIDATION_ERROR", e.Message))
            .ToArray();

        return Result<TValue>.Failure(errors);
    }

    /// <summary>
    /// Converts a ValidationResult to a Result with a value factory.
    /// The factory is only called if validation succeeds.
    /// </summary>
    public static Result<TValue> ToResult<TValue>(
        this Validation.ValidationResult validationResult,
        Func<TValue> valueFactory)
    {
        if (validationResult.IsValid)
            return Result<TValue>.Success(valueFactory());

        var errors = validationResult.Errors
            .Select(e => Error.Validation(e.Code ?? "VALIDATION_ERROR", e.Message))
            .ToArray();

        return Result<TValue>.Failure(errors);
    }

    /// <summary>
    /// Combines multiple results into a single result containing all values.
    /// </summary>
    public static Result<IReadOnlyList<TValue>> Combine<TValue>(
        this IEnumerable<Result<TValue>> results)
    {
        var resultList = results.ToList();
        var failures = resultList.Where(r => r.IsFailure).ToList();

        if (failures.Count > 0)
        {
            var allErrors = failures.SelectMany(r => r.Errors).ToArray();
            return Result<IReadOnlyList<TValue>>.Failure(allErrors);
        }

        var values = resultList.Select(r => r.Value).ToList();
        return Result<IReadOnlyList<TValue>>.Success(values);
    }

    /// <summary>
    /// Combines multiple async results.
    /// </summary>
    public static async Task<Result<IReadOnlyList<TValue>>> Combine<TValue>(
        this IEnumerable<Task<Result<TValue>>> resultTasks)
    {
        var results = await Task.WhenAll(resultTasks);
        return results.Combine();
    }

    /// <summary>
    /// Returns the first successful result, or all errors if all fail.
    /// </summary>
    public static Result<TValue> FirstOrFailure<TValue>(
        this IEnumerable<Result<TValue>> results)
    {
        var allErrors = new List<Error>();

        foreach (var result in results)
        {
            if (result.IsSuccess)
                return result;

            allErrors.AddRange(result.Errors);
        }

        return Result<TValue>.Failure(allErrors.ToArray());
    }

    /// <summary>
    /// Creates a result from a potentially null value.
    /// </summary>
    public static Result<TValue> ToResult<TValue>(this TValue? value, Error errorIfNull)
        where TValue : class
        => value is not null
            ? Result<TValue>.Success(value)
            : Result<TValue>.Failure(errorIfNull);

    /// <summary>
    /// Creates a result from a potentially null struct.
    /// </summary>
    public static Result<TValue> ToResult<TValue>(this TValue? value, Error errorIfNull)
        where TValue : struct
        => value.HasValue
            ? Result<TValue>.Success(value.Value)
            : Result<TValue>.Failure(errorIfNull);
}
