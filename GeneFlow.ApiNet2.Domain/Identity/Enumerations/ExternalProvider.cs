using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Identity.Enumerations;

/// <summary>
/// External OAuth providers.
/// </summary>
public sealed class ExternalProvider : Enumeration<ExternalProvider>
{
    /// <summary>
    /// Google OAuth provider.
    /// </summary>
    public static readonly ExternalProvider Google = new(1, nameof(Google));

    /// <summary>
    /// GitHub OAuth provider.
    /// </summary>
    public static readonly ExternalProvider GitHub = new(2, nameof(GitHub));

    private ExternalProvider(int id, string name) : base(id, name) { }
}
