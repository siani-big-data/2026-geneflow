using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Interfaces;

/// <summary>
/// Service for validating user authentication state.
/// </summary>
public interface IUserAuthenticationValidator
{
    /// <summary>
    /// Validates that a user can authenticate.
    /// </summary>
    /// <param name="user">The user to validate.</param>
    /// <returns>Success if valid; failure with error otherwise.</returns>
    Result ValidateCanAuthenticate(User user);

    /// <summary>
    /// Validates a user's password.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="password">The password to validate.</param>
    /// <returns>Success if valid; failure with error otherwise.</returns>
    Result ValidatePassword(User user, string password);
}
