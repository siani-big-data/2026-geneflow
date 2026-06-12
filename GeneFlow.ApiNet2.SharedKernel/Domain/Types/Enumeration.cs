using System.Reflection;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Types;

/// <summary>
/// Base class for creating smart enumerations with behavior.
/// Provides type-safety, additional properties, and methods beyond standard enums.
/// </summary>
/// <typeparam name="TEnum">The enumeration type itself.</typeparam>
/// <example>
/// public class OrderStatus : Enumeration&lt;OrderStatus&gt;
/// {
///     public static readonly OrderStatus Pending = new(1, nameof(Pending));
///     public static readonly OrderStatus Confirmed = new(2, nameof(Confirmed));
///     public static readonly OrderStatus Shipped = new(3, nameof(Shipped));
///     public static readonly OrderStatus Delivered = new(4, nameof(Delivered));
///     public static readonly OrderStatus Cancelled = new(5, nameof(Cancelled));
///
///     private OrderStatus(int id, string name) : base(id, name) { }
///
///     public bool CanCancel => this == Pending || this == Confirmed;
/// }
/// </example>
public abstract class Enumeration<TEnum> : IEquatable<Enumeration<TEnum>>, IComparable<Enumeration<TEnum>>
    where TEnum : Enumeration<TEnum>
{
    private static readonly Lazy<Dictionary<int, TEnum>> _byId = new(GetAllById, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<Dictionary<string, TEnum>> _byName = new(GetAllByName, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// The unique identifier.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// The name of the enumeration value.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new enumeration value with the specified ID and name.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The name of the enumeration value.</param>
    protected Enumeration(int id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Gets all defined enumeration values.
    /// </summary>
    public static IReadOnlyCollection<TEnum> GetAll() => _byId.Value.Values.ToList();

    /// <summary>
    /// Gets an enumeration value by its ID.
    /// </summary>
    public static TEnum? FromId(int id)
        => _byId.Value.GetValueOrDefault(id);

    /// <summary>
    /// Gets an enumeration value by its name (case-insensitive).
    /// </summary>
    public static TEnum? FromName(string name)
        => _byName.Value.GetValueOrDefault(name.ToUpperInvariant());

    /// <summary>
    /// Tries to get an enumeration value by its ID.
    /// </summary>
    public static bool TryFromId(int id, out TEnum? result)
    {
        result = FromId(id);
        return result is not null;
    }

    /// <summary>
    /// Tries to get an enumeration value by its name.
    /// </summary>
    public static bool TryFromName(string name, out TEnum? result)
    {
        result = FromName(name);
        return result is not null;
    }

    /// <summary>
    /// Checks if an ID is defined.
    /// </summary>
    public static bool IsDefined(int id) => _byId.Value.ContainsKey(id);

    /// <summary>
    /// Checks if a name is defined.
    /// </summary>
    public static bool IsDefined(string name) => _byName.Value.ContainsKey(name.ToUpperInvariant());

    private static Dictionary<int, TEnum> GetAllById()
    {
        return typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(TEnum))
            .Select(f => (TEnum)f.GetValue(null)!)
            .ToDictionary(e => e.Id);
    }

    private static Dictionary<string, TEnum> GetAllByName()
    {
        return typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(TEnum))
            .Select(f => (TEnum)f.GetValue(null)!)
            .ToDictionary(e => e.Name.ToUpperInvariant());
    }

    /// <inheritdoc />
    public bool Equals(Enumeration<TEnum>? other)
    {
        if (other is null)
            return false;
        return GetType() == other.GetType() && Id == other.Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is Enumeration<TEnum> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Id.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(Enumeration<TEnum>? other)
        => other is null ? 1 : Id.CompareTo(other.Id);

    /// <inheritdoc />
    public override string ToString() => Name;

    /// <summary>
    /// Determines whether two enumeration values are equal.
    /// </summary>
    public static bool operator ==(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
        => Equals(left, right);

    /// <summary>
    /// Determines whether two enumeration values are not equal.
    /// </summary>
    public static bool operator !=(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
        => !Equals(left, right);

    /// <summary>
    /// Implicitly converts an enumeration to its ID.
    /// </summary>
    public static implicit operator int(Enumeration<TEnum> enumeration) => enumeration.Id;

    /// <summary>
    /// Implicitly converts an enumeration to its name.
    /// </summary>
    public static implicit operator string(Enumeration<TEnum> enumeration) => enumeration.Name;
}
