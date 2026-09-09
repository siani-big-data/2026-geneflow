using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity;

/// <summary>
/// Domain errors for OAuth operations.
/// </summary>
public static class OAuthErrors
{
    /// <summary>Invalid or expired OAuth token.</summary>
    public static readonly Error InvalidToken = Error.Unauthorized(
        "OAuth.InvalidToken", "The OAuth token is invalid or has expired.");

    /// <summary>Provider not supported.</summary>
    public static Error ProviderNotSupported(string provider) => Error.Validation(
        "OAuth.ProviderNotSupported", $"The OAuth provider '{provider}' is not supported.");

    /// <summary>OAuth validation failed.</summary>
    public static Error ValidationFailed(string reason) => Error.Failure(
        "OAuth.ValidationFailed", $"OAuth token validation failed: {reason}");

    /// <summary>Email already associated with another account.</summary>
    public static readonly Error EmailAlreadyAssociated = Error.Conflict(
        "OAuth.EmailAlreadyAssociated", "This email is already associated with another account.");

    /// <summary>Cannot unlink the only authentication method.</summary>
    public static readonly Error CannotUnlinkOnlyAuthMethod = Error.Failure(
        "OAuth.CannotUnlinkOnlyAuthMethod", "Cannot unlink the only authentication method. Set a password first.");

    /// <summary>Provider already linked to another account.</summary>
    public static readonly Error ProviderAlreadyLinkedToAnotherAccount = Error.Conflict(
        "OAuth.ProviderAlreadyLinkedToAnotherAccount", "This OAuth account is already linked to another user.");
}
