namespace GeneFlow.ApiNet2.SharedKernel.Domain.Results;

/// <summary>
/// Represents the result of an operation that does not return a value.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the first error if the operation failed.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Gets all errors if the operation failed.
    /// </summary>
    public Error[] Errors { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="Result"/> with a single error.
    /// </summary>
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = error == Error.None ? [] : [error];
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Result"/> with multiple errors.
    /// </summary>
    protected Result(bool isSuccess, Error[] errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
        Error = errors.Length > 0 ? errors[0] : Error.None;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a failed result with multiple errors.
    /// </summary>
    public static Result Failure(params Error[] errors) => new(false, errors);

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    /// <summary>
    /// Creates a failed result with a value type.
    /// </summary>
    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);

    /// <summary>
    /// Creates a result based on a condition.
    /// </summary>
    public static Result Create(bool condition, Error error)
        => condition ? Success() : Failure(error);

    /// <summary>
    /// Creates a result from a nullable value.
    /// </summary>
    public static Result<TValue> Create<TValue>(TValue? value, Error error) where TValue : class
        => value is not null ? Success(value) : Failure<TValue>(error);

    /// <summary>
    /// Creates a result from a nullable struct.
    /// </summary>
    public static Result<TValue> Create<TValue>(TValue? value, Error error) where TValue : struct
        => value.HasValue ? Success(value.Value) : Failure<TValue>(error);

    /// <summary>
    /// Executes the appropriate action based on success or failure.
    /// </summary>
    public Result Match(Action onSuccess, Action<Error[]> onFailure)
    {
        if (IsSuccess)
            onSuccess();
        else
            onFailure(Errors);

        return this;
    }

    /// <summary>
    /// Executes the appropriate function based on success or failure.
    /// </summary>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error[], TResult> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Errors);

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result OnSuccess(Action action)
    {
        if (IsSuccess)
            action();

        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public Result OnFailure(Action<Error[]> action)
    {
        if (IsFailure)
            action(Errors);

        return this;
    }

    /// <summary>
    /// Maps a successful result to a Result with a value using a factory function.
    /// </summary>
    public Result<TValue> Map<TValue>(Func<TValue> factory)
    {
        return IsSuccess
            ? Result<TValue>.Success(factory())
            : Result<TValue>.Failure(Errors);
    }

    /// <summary>
    /// Combines multiple results. Fails if any result fails.
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        var failedResults = results.Where(r => r.IsFailure).ToArray();

        if (failedResults.Length == 0)
            return Success();

        var allErrors = failedResults.SelectMany(r => r.Errors).ToArray();
        return Failure(allErrors);
    }

    /// <inheritdoc />
    public override string ToString()
        => IsSuccess ? "Success" : $"Failure: {string.Join(", ", Errors.Select(e => e.ToString()))}";
}
