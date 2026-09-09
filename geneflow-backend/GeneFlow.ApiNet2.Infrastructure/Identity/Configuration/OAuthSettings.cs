namespace GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;

/// <summary>
/// OAuth provider configuration settings.
/// </summary>
public sealed class OAuthSettings
{
    public const string SectionName = "OAuth";

    /// <summary>
    /// Google OAuth settings.
    /// </summary>
    public OAuthProviderSettings Google { get; set; } = new();

    /// <summary>
    /// GitHub OAuth settings.
    /// </summary>
    public OAuthProviderSettings GitHub { get; set; } = new();
}

/// <summary>
/// Settings for a specific OAuth provider.
/// </summary>
public sealed class OAuthProviderSettings
{
    /// <summary>
    /// The OAuth client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
}
