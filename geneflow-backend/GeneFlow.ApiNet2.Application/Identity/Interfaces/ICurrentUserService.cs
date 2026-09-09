using GeneFlow.ApiNet2.Domain.Identity;

namespace GeneFlow.ApiNet2.Application.Identity.Interfaces;

/// <summary>
/// Service for accessing the current authenticated user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID, if authenticated.
    /// </summary>
    UserId? UserId { get; }

    /// <summary>
    /// Gets whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
