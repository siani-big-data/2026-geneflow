using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Entities;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.Domain.Orgs.Events;

namespace GeneFlow.ApiNet2.Tests.Domain.OrgsTests;

public class OrgInvitationTests
{
    private static OrgInvitationId NewInvId(long v = 1) => new(v);
    private static OrgId NewOrgId(long v = 1) => new(v);
    private static UserId NewUserId(long v = 1) => new(v);

    [Fact]
    public void Create_NormalizesEmailToLowerInvariant()
    {
        var result = OrgInvitation.Create(
            NewInvId(),
            NewOrgId(),
            "  Alice@Example.COM  ",
            invitedUserId: null,
            OrgRole.Member);

        result.IsSuccess.Should().BeTrue();
        result.Value.InvitedEmail.Should().Be("alice@example.com");
    }

    [Fact]
    public void Create_RaisesOrgMemberInvitedEvent()
    {
        var result = OrgInvitation.Create(
            NewInvId(),
            NewOrgId(),
            "bob@example.com",
            invitedUserId: null,
            OrgRole.Admin);

        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().ContainSingle(e => e is OrgMemberInvitedEvent);
        result.Value.Status.Should().Be(InvitationStatus.Pending);
        result.Value.Token.Should().NotBeNullOrWhiteSpace();
        result.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Accept_WhenPending_SetsStatusAccepted_AndRaisesEvent()
    {
        var invitation = OrgInvitation.Create(
            NewInvId(), NewOrgId(), "a@x.com", invitedUserId: null, OrgRole.Member).Value;
        invitation.ClearDomainEvents();

        var result = invitation.Accept(NewUserId(7));

        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Accepted);
        invitation.AcceptedByUserId.Should().Be(NewUserId(7));
        invitation.DomainEvents.Should().ContainSingle(e => e is OrgInvitationAcceptedEvent);
    }

    [Fact]
    public void Accept_WhenAlreadyAccepted_ReturnsFailure()
    {
        var invitation = OrgInvitation.Create(
            NewInvId(), NewOrgId(), "a@x.com", invitedUserId: null, OrgRole.Member).Value;
        invitation.Accept(NewUserId(7));

        var second = invitation.Accept(NewUserId(7));

        second.IsFailure.Should().BeTrue();
        second.Error.Should().Be(OrgErrors.OrgInvitation.NotPending);
    }

    [Fact]
    public void Accept_WhenExpired_TransitionsToExpired_AndReturnsFailure()
    {
        // ttl of zero gives an expiry of "now"; by the time we Accept, the
        // clock will have moved past it.
        var invitation = OrgInvitation.Create(
            NewInvId(), NewOrgId(), "a@x.com", invitedUserId: null,
            OrgRole.Member, ttl: TimeSpan.Zero).Value;
        Thread.Sleep(5);

        var result = invitation.Accept(NewUserId(7));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrgErrors.OrgInvitation.Expired);
        invitation.Status.Should().Be(InvitationStatus.Expired);
    }

    [Fact]
    public void Accept_WhenInvitedUserIdMismatch_ReturnsFailure()
    {
        var invitedUser = NewUserId(7);
        var otherUser = NewUserId(8);
        var invitation = OrgInvitation.Create(
            NewInvId(), NewOrgId(), "a@x.com", invitedUser, OrgRole.Member).Value;

        var result = invitation.Accept(otherUser);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrgErrors.OrgInvitation.WrongUser);
        invitation.Status.Should().Be(InvitationStatus.Pending);
    }

    [Fact]
    public void Decline_WhenPending_Succeeds()
    {
        var invitation = OrgInvitation.Create(
            NewInvId(), NewOrgId(), "a@x.com", invitedUserId: null, OrgRole.Member).Value;

        var result = invitation.Decline(NewUserId(7));

        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Declined);
        invitation.DomainEvents.Should().Contain(e => e is OrgInvitationDeclinedEvent);
    }

    [Fact]
    public void Revoke_WhenPending_Succeeds()
    {
        var invitation = OrgInvitation.Create(
            NewInvId(), NewOrgId(), "a@x.com", invitedUserId: null, OrgRole.Member).Value;

        var result = invitation.Revoke();

        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Revoked);
    }
}
