using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Helpers to short-circuit endpoints when the current request is unauthenticated.
/// Centralizes the duplicated null-check on <see cref="ICurrentUserService.UserId"/>
/// that appears in every authenticated endpoint.
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>
    /// Attempts to extract the authenticated user identifier. Returns <c>null</c> when
    /// the caller is authenticated and the <paramref name="userId"/> is populated; or an
    /// <see cref="IResult"/> representing an HTTP 401 Unauthorized response that the
    /// endpoint should return immediately.
    /// </summary>
    /// <param name="currentUser">Service exposing the current authenticated user.</param>
    /// <param name="userId">When this method returns <c>null</c>, contains the authenticated user identifier.</param>
    /// <returns><c>null</c> on success or <see cref="Results.Unauthorized"/> when the request is unauthenticated.</returns>
    public static IResult? RequireAuthenticated(this ICurrentUserService currentUser, out UserId userId)
    {
        if (currentUser.UserId is null)
        {
            userId = null!;
            return Results.Unauthorized();
        }

        userId = currentUser.UserId;
        return null;
    }
}
