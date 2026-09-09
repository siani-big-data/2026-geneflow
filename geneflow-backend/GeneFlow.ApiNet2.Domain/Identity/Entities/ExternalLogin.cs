using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Identity.Entities;

/// <summary>
/// Represents an external OAuth login linked to a user account.
/// </summary>
public sealed class ExternalLogin : Entity<Guid>
{
    /// <summary>
    /// Gets the OAuth provider (e.g., Google, GitHub).
    /// </summary>
    public ExternalProvider Provider { get; private set; }

    /// <summary>
    /// Gets the unique identifier from the external provider.
    /// </summary>
    public string ProviderKey { get; private set; }

    /// <summary>
    /// Gets the display name from the external provider, if available.
    /// </summary>
    public string? ProviderDisplayName { get; private set; }

    /// <summary>
    /// Gets the timestamp when the external login was linked.
    /// </summary>
    public DateTime LinkedAt { get; private set; }

    private ExternalLogin() : base()
    {
        Provider = null!;
        ProviderKey = null!;
    }

    private ExternalLogin(
        Guid id,
        ExternalProvider provider,
        string providerKey,
        string? providerDisplayName) : base(id)
    {
        Provider = provider;
        ProviderKey = providerKey;
        ProviderDisplayName = providerDisplayName;
        LinkedAt = DateTime.UtcNow;
    }

    internal static ExternalLogin Create(
        ExternalProvider provider,
        string providerKey,
        string? providerDisplayName = null)
    {
        return new ExternalLogin(
            Guid.NewGuid(),
            provider,
            providerKey,
            providerDisplayName);
    }

    internal void UpdateDisplayName(string? displayName)
    {
        ProviderDisplayName = displayName;
    }
}
