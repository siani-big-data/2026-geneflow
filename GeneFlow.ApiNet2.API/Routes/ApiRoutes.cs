namespace GeneFlow.ApiNet2.API.Routes;

/// <summary>
/// Centralized API route definitions.
/// </summary>
public static class ApiRoutes
{
    /// <summary>Base path for all public API endpoints.</summary>
    public const string Base = "/api/v1";

    /// <summary>Base path for internal/worker API endpoints.</summary>
    public const string Internal = "/api/v1/internal";

    /// <summary>Authentication routes.</summary>
    public static class Auth
    {
        /// <summary>Base path for auth endpoints.</summary>
        public const string Base = $"{ApiRoutes.Base}/auth";

        /// <summary>Base path for OAuth endpoints.</summary>
        public const string OAuth = $"{Base}/oauth";
    }

    /// <summary>User management routes.</summary>
    public static class Users
    {
        /// <summary>Base path for user endpoints.</summary>
        public const string Base = $"{ApiRoutes.Base}/users";
    }
}
