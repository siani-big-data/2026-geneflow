using System.Text.RegularExpressions;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

/// <summary>
/// Represents a person's name (first and last name).
/// </summary>
public sealed partial class PersonName : ValueObject
{
    /// <summary>Minimum length of first name.</summary>
    public const int FirstNameMinLength = 2;

    /// <summary>Maximum length of first name.</summary>
    public const int FirstNameMaxLength = 100;

    /// <summary>Minimum length of last name (if provided).</summary>
    public const int LastNameMinLength = 2;

    /// <summary>Maximum length of last name.</summary>
    public const int LastNameMaxLength = 100;

    private static readonly Regex NameRegex = GeneratedNameRegex();

    /// <summary>Gets the first name.</summary>
    public string FirstName { get; }

    /// <summary>Gets the last name (optional).</summary>
    public string? LastName { get; }

    /// <summary>Gets the full name.</summary>
    public string FullName => string.IsNullOrWhiteSpace(LastName)
        ? FirstName
        : $"{FirstName} {LastName}";

    /// <summary>Gets the initials.</summary>
    public string Initials
    {
        get
        {
            var first = !string.IsNullOrEmpty(FirstName) ? FirstName[0].ToString().ToUpperInvariant() : "";
            var last = !string.IsNullOrEmpty(LastName) ? LastName[0].ToString().ToUpperInvariant() : "";
            return first + last;
        }
    }

    private PersonName(string firstName, string? lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    /// <summary>
    /// Creates a validated PersonName.
    /// </summary>
    public static Result<PersonName> Create(string? firstName, string? lastName = null)
    {
        var trimmedFirstName = firstName?.Trim() ?? string.Empty;
        var trimmedLastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim();

        // Validate first name
        if (string.IsNullOrWhiteSpace(trimmedFirstName))
            return Result.Failure<PersonName>(ProfileErrors.FirstNameRequired);

        if (trimmedFirstName.Length < FirstNameMinLength)
            return Result.Failure<PersonName>(ProfileErrors.FirstNameTooShort(FirstNameMinLength));

        if (trimmedFirstName.Length > FirstNameMaxLength)
            return Result.Failure<PersonName>(ProfileErrors.FirstNameTooLong(FirstNameMaxLength));

        if (!NameRegex.IsMatch(trimmedFirstName))
            return Result.Failure<PersonName>(ProfileErrors.FirstNameInvalidFormat);

        // Validate last name (if provided)
        if (trimmedLastName is not null)
        {
            if (trimmedLastName.Length < LastNameMinLength)
                return Result.Failure<PersonName>(ProfileErrors.LastNameTooShort(LastNameMinLength));

            if (trimmedLastName.Length > LastNameMaxLength)
                return Result.Failure<PersonName>(ProfileErrors.LastNameTooLong(LastNameMaxLength));

            if (!NameRegex.IsMatch(trimmedLastName))
                return Result.Failure<PersonName>(ProfileErrors.LastNameInvalidFormat);
        }

        return new PersonName(trimmedFirstName, trimmedLastName);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }

    /// <inheritdoc />
    public override string ToString() => FullName;

    [GeneratedRegex(@"^[\p{L}\s\-']+$", RegexOptions.Compiled)]
    private static partial Regex GeneratedNameRegex();
}
