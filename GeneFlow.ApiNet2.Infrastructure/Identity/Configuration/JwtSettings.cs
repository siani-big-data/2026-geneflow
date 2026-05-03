namespace GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;

/// <summary>
/// JWT configuration settings.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Gets or sets the secret key.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Gets or sets the token issuer.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets the token audience.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Gets or sets access token expiration in minutes.</summary>
    public int AccessTokenExpirationMinutes { get; set; } = 120;

    /// <summary>Gets or sets refresh token expiration in days.</summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
