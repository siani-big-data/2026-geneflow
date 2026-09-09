using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Orgs.Enumerations;

/// <summary>
/// State machine for an <see cref="Entities.OrgInvitation"/>.
/// Terminal states are Accepted, Declined, Expired and Revoked.
/// </summary>
public sealed class InvitationStatus : Enumeration<InvitationStatus>
{
    /// <summary>
    /// Invitation is open and may be accepted or declined.
    /// </summary>
    public static readonly InvitationStatus Pending = new(1, nameof(Pending));

    /// <summary>
    /// Recipient accepted; a member row was created.
    /// </summary>
    public static readonly InvitationStatus Accepted = new(2, nameof(Accepted));

    /// <summary>
    /// Recipient declined explicitly.
    /// </summary>
    public static readonly InvitationStatus Declined = new(3, nameof(Declined));

    /// <summary>
    /// Invitation passed its expiry time without being accepted.
    /// </summary>
    public static readonly InvitationStatus Expired = new(4, nameof(Expired));

    /// <summary>
    /// An org owner/admin revoked the invitation before the recipient acted.
    /// </summary>
    public static readonly InvitationStatus Revoked = new(5, nameof(Revoked));

    private InvitationStatus(int id, string name) : base(id, name) { }
}
