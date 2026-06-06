using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.Domain.Orgs.Events;
using OrgAggregate = GeneFlow.ApiNet2.Domain.Orgs.Org;

namespace GeneFlow.ApiNet2.Tests.Domain.OrgsTests;

public class OrgTests
{
    private static OrgId NewOrgId(long v = 1) => new(v);
    private static UserId NewUserId(long v = 1) => new(v);

    [Fact]
    public void Create_WithValidInputs_SetsCreatorAsOwner_AndRaisesOrgCreatedEvent()
    {
        var creator = NewUserId(42);

        var result = OrgAggregate.Create(NewOrgId(), "acme", "Acme Inc", creator);

        result.IsSuccess.Should().BeTrue();
        var org = result.Value;
        org.Handle.Should().Be("acme");
        org.Name.Should().Be("Acme Inc");
        org.Visibility.Should().Be(OrgVisibility.Public);
        org.Members.Should().HaveCount(1);
        org.GetMember(creator).Should().NotBeNull();
        org.GetMember(creator)!.Role.Should().Be(OrgRole.Owner);
        org.DomainEvents.Should().ContainSingle(e => e is OrgCreatedEvent);
    }

    [Fact]
    public void Create_WithInvalidHandle_ReturnsFailure()
    {
        // Uppercase letters are not allowed by the handle regex.
        var result = OrgAggregate.Create(NewOrgId(), "Bad Handle!", "Acme", NewUserId());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrgErrors.HandleInvalid);
    }

    [Fact]
    public void Create_WithEmptyName_ReturnsFailure()
    {
        var result = OrgAggregate.Create(NewOrgId(), "acme", "   ", NewUserId());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrgErrors.NameRequired);
    }

    [Fact]
    public void AddMember_NewUser_AddsMember()
    {
        var org = OrgAggregate.Create(NewOrgId(), "acme", "Acme", NewUserId(1)).Value;
        var newUser = NewUserId(2);

        var result = org.AddMember(newUser, OrgRole.Member);

        result.IsSuccess.Should().BeTrue();
        org.Members.Should().HaveCount(2);
        org.IsMember(newUser).Should().BeTrue();
        org.GetMember(newUser)!.Role.Should().Be(OrgRole.Member);
    }

    [Fact]
    public void AddMember_ExistingUser_IsIdempotent()
    {
        var creator = NewUserId(1);
        var org = OrgAggregate.Create(NewOrgId(), "acme", "Acme", creator).Value;

        // The creator is already an Owner. Re-adding must succeed without
        // creating a duplicate row and without demoting the existing one.
        var result = org.AddMember(creator, OrgRole.Member);

        result.IsSuccess.Should().BeTrue();
        org.Members.Should().HaveCount(1);
        org.GetMember(creator)!.Role.Should().Be(OrgRole.Owner);
    }

    [Fact]
    public void ChangeMemberRole_DemotingLastOwner_ReturnsFailuREDACTED()
    {
        var creator = NewUserId(1);
        var org = OrgAggregate.Create(NewOrgId(), "acme", "Acme", creator).Value;

        var result = org.ChangeMemberRole(creator, OrgRole.Member, creator);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrgErrors.MustHaveOwner);
        org.GetMember(creator)!.Role.Should().Be(OrgRole.Owner);
    }

    [Fact]
    public void ChangeMemberRole_DemotingOneOfMultipleOwners_Succeeds()
    {
        var owner1 = NewUserId(1);
        var owner2 = NewUserId(2);
        var org = OrgAggregate.Create(NewOrgId(), "acme", "Acme", owner1).Value;
        org.AddMember(owner2, OrgRole.Owner);

        var result = org.ChangeMemberRole(owner2, OrgRole.Member, owner1);

        result.IsSuccess.Should().BeTrue();
        org.GetMember(owner2)!.Role.Should().Be(OrgRole.Member);
        org.DomainEvents.Should().Contain(e => e is OrgMemberRoleChangedEvent);
    }

    [Fact]
    public void RemoveMember_LastOwner_ReturnsFailuREDACTED()
    {
        var creator = NewUserId(1);
        var org = OrgAggregate.Create(NewOrgId(), "acme", "Acme", creator).Value;

        var result = org.RemoveMember(creator, creator);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrgErrors.MustHaveOwner);
        org.IsMember(creator).Should().BeTrue();
    }

    [Fact]
    public void RemoveMember_NonOwner_Succeeds()
    {
        var creator = NewUserId(1);
        var member = NewUserId(2);
        var org = OrgAggregate.Create(NewOrgId(), "acme", "Acme", creator).Value;
        org.AddMember(member, OrgRole.Member);

        var result = org.RemoveMember(member, creator);

        result.IsSuccess.Should().BeTrue();
        org.IsMember(member).Should().BeFalse();
        org.DomainEvents.Should().Contain(e => e is OrgMemberRemovedEvent);
    }
}
