using System.Diagnostics.CodeAnalysis;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Types;

/// <summary>
/// Represents an optional value that may or may not be present.
/// Alternative to null that makes optionality explicit in the type system.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public readonly struct Maybe<T> : IEquatable<Maybe<T>>
{
    private readonly T? _value;

    /// <summary>
    /// Whether a value is present.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_value))]
    public bool HasValue { get; }

    /// <summary>
    /// Whether no value is present.
    /// </summary>
    public bool HasNoValue => !HasValue;

    /// <summary>
    /// Gets the value if present, throws if not.
    /// </summary>
    public T Value => HasValue
        ? _value
        : throw new InvalidOperationException("Maybe has no value.");

    private Maybe(T value)
    {
        _value = value;
        HasValue = true;
    }

    /// <summary>
    /// Creates a Maybe with a value.
    /// </summary>
    public static Maybe<T> Some(T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value), "Cannot create Some with null value. Use None instead.");

        return new Maybe<T>(value);
    }

    /// <summary>
    /// Creates a Maybe without a value.
    /// </summary>
    public static Maybe<T> None => default;

    /// <summary>
    /// Creates a Maybe from a potentially null value.
    /// </summary>
    public static Maybe<T> From(T? value)
        => value is null ? None : Some(value);

    /// <summary>
    /// Gets the value or a default if not present.
    /// </summary>
    public T GetValueOrDefault(T defaultValue = default!)
        => HasValue ? _value : defaultValue;

    /// <summary>
    /// Gets the value or throws a custom exception.
    /// </summary>
    public T GetValueOrThrow(Exception exception)
        => HasValue ? _value : throw exception;

    /// <summary>
    /// Gets the value or throws with a custom message.
    /// </summary>
    public T GetValueOrThrow(string errorMessage)
        => HasValue ? _value : throw new InvalidOperationException(errorMessage);

    /// <summary>
    /// Tries to get the value.
    /// </summary>
    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = _value;
        return HasValue;
    }

    /// <summary>
    /// Transforms the value if present.
    /// </summary>
    public Maybe<TResult> Map<TResult>(Func<T, TResult> mapper)
        => HasValue ? Maybe<TResult>.Some(mapper(_value)) : Maybe<TResult>.None;

    /// <summary>
    /// Transforms the value if present (async).
    /// </summary>
    public async Task<Maybe<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> mapper)
        => HasValue ? Maybe<TResult>.Some(await mapper(_value)) : Maybe<TResult>.None;

    /// <summary>
    /// Chains another Maybe-returning operation.
    /// </summary>
    public Maybe<TResult> Bind<TResult>(Func<T, Maybe<TResult>> binder)
        => HasValue ? binder(_value) : Maybe<TResult>.None;

    /// <summary>
    /// Chains another Maybe-returning operation (async).
    /// </summary>
    public async Task<Maybe<TResult>> BindAsync<TResult>(Func<T, Task<Maybe<TResult>>> binder)
        => HasValue ? await binder(_value) : Maybe<TResult>.None;

    /// <summary>
    /// Filters the value based on a predicate.
    /// </summary>
    public Maybe<T> Where(Func<T, bool> predicate)
        => HasValue && predicate(_value) ? this : None;

    /// <summary>
    /// Executes an action if value is present.
    /// </summary>
    public Maybe<T> OnSome(Action<T> action)
    {
        if (HasValue)
            action(_value);

        return this;
    }

    /// <summary>
    /// Executes an action if no value is present.
    /// </summary>
    public Maybe<T> OnNone(Action action)
    {
        if (HasNoValue)
            action();

        return this;
    }

    /// <summary>
    /// Pattern matches on the Maybe.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone)
        => HasValue ? onSome(_value) : onNone();

    /// <summary>
    /// Returns this Maybe if it has a value, otherwise returns the fallback.
    /// </summary>
    public Maybe<T> Or(Maybe<T> fallback)
        => HasValue ? this : fallback;

    /// <summary>
    /// Returns this Maybe if it has a value, otherwise creates a fallback.
    /// </summary>
    public Maybe<T> Or(Func<Maybe<T>> fallbackFactory)
        => HasValue ? this : fallbackFactory();

    /// <inheritdoc />
    public bool Equals(Maybe<T> other)
    {
        if (HasNoValue && other.HasNoValue)
            return true;

        if (HasNoValue || other.HasNoValue)
            return false;

        return EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is Maybe<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => HasValue ? _value.GetHashCode() : 0;

    /// <inheritdoc />
    public override string ToString()
        => HasValue ? $"Some({_value})" : "None";

    /// <summary>Determines whether two Maybe instances are equal.</summary>
    public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

    /// <summary>Determines whether two Maybe instances are not equal.</summary>
    public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);

    /// <summary>Implicitly converts a value to a Maybe.</summary>
    public static implicit operator Maybe<T>(T? value) => From(value);
}

/// <summary>
/// Static helper methods for Maybe.
/// </summary>
public static class Maybe
{
    /// <summary>
    /// Creates a Maybe with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A Maybe containing the value.</returns>
    public static Maybe<T> Some<T>(T value) => Maybe<T>.Some(value);

    /// <summary>
    /// Creates a Maybe without a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>An empty Maybe.</returns>
    public static Maybe<T> None<T>() => Maybe<T>.None;

    /// <summary>
    /// Creates a Maybe from a potentially null value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The potentially null value.</param>
    /// <returns>A Maybe containing the value if non-null, otherwise None.</returns>
    public static Maybe<T> From<T>(T? value) => Maybe<T>.From(value);
}
