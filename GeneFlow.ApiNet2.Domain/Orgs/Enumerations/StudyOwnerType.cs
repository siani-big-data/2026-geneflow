using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Orgs.Enumerations;

/// <summary>
/// Discriminator for the type of principal that owns a Study.
/// A study can be owned either by an individual <see cref="Identity.User"/>
/// or by an <see cref="Org"/>.
/// </summary>
public sealed class StudyOwnerType : Enumeration<StudyOwnerType>
{
    /// <summary>
    /// Owner is an individual user. <c>Study.OwnerId</c> is a UserId.
    /// </summary>
    public static readonly StudyOwnerType User = new(1, nameof(User));

    /// <summary>
    /// Owner is an organisation. <c>Study.OwnerId</c> is an OrgId.
    /// </summary>
    public static readonly StudyOwnerType Org = new(2, nameof(Org));

    private StudyOwnerType(int id, string name) : base(id, name) { }
}
