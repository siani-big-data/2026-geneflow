using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Identity.Enumerations;

/// <summary>
/// User roles in the system.
/// </summary>
public sealed class Role : Enumeration<Role>
{
    /// <summary>
    /// Standard user role with basic permissions.
    /// </summary>
    public static readonly Role User = new(1, nameof(User));

    /// <summary>
    /// Administrator role with full system permissions.
    /// </summary>
    public static readonly Role Admin = new(2, nameof(Admin));

    private Role(int id, string name) : base(id, name) { }
}
