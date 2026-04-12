using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Studies.Enumerations;

/// <summary>
/// Status of a study invitation.
/// </summary>
public sealed class InvitationStatus : Enumeration<InvitationStatus>
{
    public static readonly InvitationStatus Pending = new(1, nameof(Pending), "Pending");
    public static readonly InvitationStatus Accepted = new(2, nameof(Accepted), "Accepted");
    public static readonly InvitationStatus Declined = new(3, nameof(Declined), "Declined");
    public static readonly InvitationStatus Expired = new(4, nameof(Expired), "Expired");
    public static readonly InvitationStatus Cancelled = new(5, nameof(Cancelled), "Cancelled");

    public string DisplayName { get; }

    private InvitationStatus(int id, string name, string displayName) : base(id, name)
    {
        DisplayName = displayName;
    }

    public bool IsFinal => this != Pending;
    public bool CanBeResent => this == Pending || this == Expired;
}
