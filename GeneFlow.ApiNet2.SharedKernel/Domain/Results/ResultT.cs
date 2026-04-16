using System.Diagnostics.CodeAnalysis;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Results;

/// <summary>
/// Represents the result of an operation that returns a value.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    /// <summary>
    /// Gets the value if the result is successful.
    /// Throws if the result is a failure.
    /// </summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access value of a failed result. Error: {Error}");

    private Result(TValue value) : base(true, Error.None)
    {
        _value = value;
    }

    private Result(Error error) : base(false, error)
    {
        _value = default;
    }

    private Result(Error[] errors) : base(false, errors)
    {
        _value = default;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static Result<TValue> Success(TValue value) => new(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static new Result<TValue> Failure(Error error) => new(error);

    /// <summary>
    /// Creates a failed result with multiple errors.
    /// </summary>
    public static new Result<TValue> Failure(params Error[] errors) => new(errors);

    /// <summary>
    /// Gets the value or a default value if the result is a failure.
    /// </summary>
    public TValue? GetValueOrDefault(TValue? defaultValue = default)
        => IsSuccess ? _value : defaultValue;

    /// <summary>
    /// Tries to get the value.
    /// </summary>
    public bool TryGetValue([NotNullWhen(true)] out TValue? value)
    {
        value = _value;
        return IsSuccess;
    }

    /// <summary>
    /// Transforms the value if successful.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<TValue, TNew> mapper)
        => IsSuccess ? Result<TNew>.Success(mapper(_value!)) : Result<TNew>.Failure(Errors);

    /// <summary>
    /// Transforms the value if successful (async).
    /// </summary>
    public async Task<Result<TNew>> MapAsync<TNew>(Func<TValue, Task<TNew>> mapper)
        => IsSuccess ? Result<TNew>.Success(await mapper(_value!)) : Result<TNew>.Failure(Errors);

    /// <summary>
    /// Chains another result-returning operation if successful.
    /// </summary>
    public Result<TNew> Bind<TNew>(Func<TValue, Result<TNew>> binder)
        => IsSuccess ? binder(_value!) : Result<TNew>.Failure(Errors);

    /// <summary>
    /// Chains another result-returning operation if successful (async).
    /// </summary>
    public async Task<Result<TNew>> BindAsync<TNew>(Func<TValue, Task<Result<TNew>>> binder)
        => IsSuccess ? await binder(_value!) : Result<TNew>.Failure(Errors);

    /// <summary>
    /// Chains a void result-returning operation if successful.
    /// </summary>
    public Result Bind(Func<TValue, Result> binder)
        => IsSuccess ? binder(_value!) : Result.Failure(Errors);

    /// <summary>
    /// Executes the appropriate function based on success or failure.
    /// </summary>
    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<Error[], TResult> onFailure)
        => IsSuccess ? onSuccess(_value!) : onFailure(Errors);

    /// <summary>
    /// Executes the appropriate action based on success or failure.
    /// </summary>
    public Result<TValue> Match(Action<TValue> onSuccess, Action<Error[]> onFailure)
    {
        if (IsSuccess)
            onSuccess(_value!);
        else
            onFailure(Errors);

        return this;
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public new Result<TValue> OnSuccess(Action action)
    {
        if (IsSuccess)
            action();

        return this;
    }

    /// <summary>
    /// Executes an action with the value if the result is successful.
    /// </summary>
    public Result<TValue> OnSuccess(Action<TValue> action)
    {
        if (IsSuccess)
            action(_value!);

        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public new Result<TValue> OnFailure(Action<Error[]> action)
    {
        if (IsFailure)
            action(Errors);

        return this;
    }

    /// <summary>
    /// Ensures a condition is met, otherwise returns a failure.
    /// </summary>
    public Result<TValue> Ensure(Func<TValue, bool> predicate, Error error)
    {
        if (IsFailure)
            return this;

        return predicate(_value!) ? this : Failure(error);
    }

    /// <summary>
    /// Ensures a condition is met (async), otherwise returns a failure.
    /// </summary>
    public async Task<Result<TValue>> EnsureAsync(Func<TValue, Task<bool>> predicate, Error error)
    {
        if (IsFailure)
            return this;

        return await predicate(_value!) ? this : Failure(error);
    }

    /// <summary>
    /// Implicit conversion from value to successful result.
    /// </summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);

    /// <summary>
    /// Implicit conversion from error to failed result.
    /// </summary>
    public static implicit operator Result<TValue>(Error error) => Failure(error);

    /// <inheritdoc />
    public override string ToString()
        => IsSuccess ? $"Success: {_value}" : $"Failure: {string.Join(", ", Errors.Select(e => e.ToString()))}";
}
