using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

/// <summary>
/// Represents institution information.
/// </summary>
public sealed class Institution : ValueObject
{
    /// <summary>Maximum length of institution name.</summary>
    public const int NameMaxLength = 200;

    /// <summary>Maximum length of institution department.</summary>
    public const int DepartmentMaxLength = 200;

    /// <summary>Gets the institution name.</summary>
    public string? Name { get; }

    /// <summary>Gets the institution department.</summary>
    public string? Department { get; }

    /// <summary>Gets the display name (Name + Department if present).</summary>
    public string? DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name))
                return null;

            return string.IsNullOrWhiteSpace(Department)
                ? Name
                : $"{Name}, {Department}";
        }
    }

    private Institution(string? name, string? department)
    {
        Name = name;
        Department = department;
    }

    /// <summary>
    /// Creates a validated Institution.
    /// </summary>
    public static Result<Institution> Create(string? name, string? department = null)
    {
        var trimmedName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        var trimmedDepartment = string.IsNullOrWhiteSpace(department) ? null : department.Trim();

        if (trimmedName is not null && trimmedName.Length > NameMaxLength)
            return Result.Failure<Institution>(ProfileErrors.InstitutionNameTooLong(NameMaxLength));

        if (trimmedDepartment is not null && trimmedDepartment.Length > DepartmentMaxLength)
            return Result.Failure<Institution>(ProfileErrors.InstitutionDepartmentTooLong(DepartmentMaxLength));

        return new Institution(trimmedName, trimmedDepartment);
    }

    /// <summary>
    /// Creates an empty Institution.
    /// </summary>
    public static Institution Empty => new(null, null);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Department;
    }

    /// <inheritdoc />
    public override string ToString() => DisplayName ?? string.Empty;
}
