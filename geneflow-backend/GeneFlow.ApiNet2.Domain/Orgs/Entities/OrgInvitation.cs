using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.Domain.Orgs.Events;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Orgs.Entities;

/// <summary>
/// An invitation for a user (identified by email and optionally a UserId) to
/// join an <see cref="Org"/> with a given role. Operates as a state machine
/// over <see cref="Enumerations.InvitationStatus"/>.
///
/// Modelled as its own aggregate root so that listing / accepting an invite
/// does not have to lock the parent Org aggregate.
/// </summary>
public sealed class OrgInvitation : FullAuditableAggregateRoot<OrgInvitationId>
{
    /// <summary>
    /// Default expiry window for new invitations.
    /// </summary>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(7);

    public OrgId OrgId { get; private set; } = null!;
    public string InvitedEmail { get; private set; } = null!;
    public UserId? InvitedUserId { get; private set; }
    public OrgRole Role { get; private set; } = null!;

    /// <summary>
    /// Opaque token used in the accept/decline URL. Stored as the string form
    /// of a Guid; uniquely indexed at the DB level.
    /// </summary>
    public string Token { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }
    public InvitationStatus Status { get; private set; } = null!;
    public UserId? AcceptedByUserId { get; private set; }

    private OrgInvitation() : base() { }

    private OrgInvitation(
        OrgInvitationId id,
        OrgId orgId,
        string invitedEmail,
        UserId? invitedUserId,
        OrgRole role,
        string token,
        DateTime expiresAt) : base(id)
    {
        OrgId = orgId;
        InvitedEmail = invitedEmail;
        InvitedUserId = invitedUserId;
        Role = role;
        Token = token;
        ExpiresAt = expiresAt;
        Status = InvitationStatus.Pending;

        InitializeCreatedAt();
    }

    /// <summary>
    /// Creates a new invitation. Generates a fresh Guid token and sets the
    /// expiry window (<paramref name="ttl"/> or <see cref="DefaultTtl"/>).
    /// </summary>
    public static Result<OrgInvitation> Create(
        OrgInvitationId id,
        OrgId orgId,
        string invitedEmail,
        UserId? invitedUserId,
        OrgRole role,
        TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(invitedEmail))
            return Result.Failure<OrgInvitation>(OrgErrors.OrgInvitation.EmailRequired);

        var normalizedEmail = invitedEmail.Trim().ToLowerInvariant();
        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.Add(ttl ?? DefaultTtl);

        var invitation = new OrgInvitation(
            id,
            orgId,
            normalizedEmail,
            invitedUserId,
            role,
            token,
            expiresAt);

        invitation.RaiseDomainEvent(new OrgMemberInvitedEvent(id, orgId, normalizedEmail, role));

        return invitation;
    }

    /// <summary>
    /// Accepts the invitation on behalf of <paramref name="userId"/>.
    /// Enforces: must be Pending; must not be past <see cref="ExpiresAt"/>;
    /// if an <see cref="InvitedUserId"/> was specified, it must match.
    /// </summary>
    public Result Accept(UserId userId)
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure(OrgErrors.OrgInvitation.NotPending);

        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = InvitationStatus.Expired;
            SetModified(userId.ToString());
            return Result.Failure(OrgErrors.OrgInvitation.Expired);
        }

        if (InvitedUserId is not null && !InvitedUserId.Equals(userId))
            return Result.Failure(OrgErrors.OrgInvitation.WrongUser);

        Status = InvitationStatus.Accepted;
        AcceptedByUserId = userId;
        SetModified(userId.ToString());

        RaiseDomainEvent(new OrgInvitationAcceptedEvent(Id, OrgId, userId));
        return Result.Success();
    }

    /// <summary>
    /// Declines the invitation. Same state-machine rules as Accept.
    /// </summary>
    public Result Decline(UserId userId)
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure(OrgErrors.OrgInvitation.NotPending);

        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = InvitationStatus.Expired;
            SetModified(userId.ToString());
            return Result.Failure(OrgErrors.OrgInvitation.Expired);
        }

        if (InvitedUserId is not null && !InvitedUserId.Equals(userId))
            return Result.Failure(OrgErrors.OrgInvitation.WrongUser);

        Status = InvitationStatus.Declined;
        SetModified(userId.ToString());

        RaiseDomainEvent(new OrgInvitationDeclinedEvent(Id, OrgId, userId));
        return Result.Success();
    }

    /// <summary>
    /// Revokes a still-pending invitation. Org admins call this to take back
    /// an invitation that hasn't been actioned yet.
    /// </summary>
    public Result Revoke()
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure(OrgErrors.OrgInvitation.NotPending);

        Status = InvitationStatus.Revoked;
        SetModified();
        return Result.Success();
    }

    /// <summary>
    /// Marks the invitation as expired. Idempotent so a background sweeper
    /// can call this without re-reading state first.
    /// </summary>
    public Result MarkExpired()
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure(OrgErrors.OrgInvitation.NotPending);

        Status = InvitationStatus.Expired;
        SetModified();
        return Result.Success();
    }
}
