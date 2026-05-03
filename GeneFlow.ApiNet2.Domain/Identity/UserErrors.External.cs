using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity;

/// <summary>
/// Errors related to external login providers (OAuth) and role assignments.
/// Partial of <see cref="UserErrors"/>.
/// </summary>
public static partial class UserErrors
{
    /// <summary>Role already assigned.</summary>
    public static readonly Error RoleAlreadyAssigned = Error.Conflict(
        "User.RoleAlreadyAssigned", "User already has this role.");

    /// <summary>Role not assigned.</summary>
    public static readonly Error RoleNotAssigned = Error.Failure(
        "User.RoleNotAssigned", "User does not have this role.");

    /// <summary>External login already linked.</summary>
    public static readonly Error ExternalLoginAlreadyLinked = Error.Conflict(
        "User.ExternalLoginAlreadyLinked", "This external login is already linked to an account.");

    /// <summary>External login not found.</summary>
    public static readonly Error ExternalLoginNotFound = Error.NotFound(
        "User.ExternalLoginNotFound", "External login was not found.");
}
