using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Commands.AcceptOrgInvitation;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Entities;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using OrgAggregate = GeneFlow.ApiNet2.Domain.Orgs.Org;

namespace GeneFlow.ApiNet2.Tests.Application.OrgsTests;

public class AcceptOrgInvitationCommandHandlerTests
{
    private readonly IOrgRepository _orgRepo = Substitute.For<IOrgRepository>();
    private readonly IOrgInvitationRepository _invitationRepo = Substitute.For<IOrgInvitationRepository>();
    private readonly IOrgUnitOfWork _uow = Substitute.For<IOrgUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly AcceptOrgInvitationCommandHandler _handler;

    public AcceptOrgInvitationCommandHandlerTests()
    {
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _currentUser.UserId.Returns(new UserId(99));

        _handler = new AcceptOrgInvitationCommandHandler(
            _orgRepo, _invitationRepo, _uow, _currentUser);
    }

    [Fact]
    public async Task Handle_PendingInvitation_AcceptsAndAddsMember()
    {
        var orgId = new OrgId(1);
        var creator = new UserId(1);
        var org = OrgAggregate.Create(orgId, "acme", "Acme", creator).Value;

        var invitation = OrgInvitation.Create(
            new OrgInvitationId(7),
            orgId,
            "new@x.com",
            invitedUserId: null,
            OrgRole.Member).Value;

        _invitationRepo.GetByIdAsync(Arg.Any<OrgInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);
        _orgRepo.GetByIdAsync(orgId, Arg.Any<CancellationToken>())
            .Returns(org);

        var command = new AcceptOrgInvitationCommand(invitation.Id.ToString());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Accepted);
        org.IsMember(new UserId(99)).Should().BeTrue();
        _orgRepo.Received(1).Update(org);
        _invitationRepo.Received().Update(invitation);
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvitationNotFound_ReturnsFailure()
    {
        _invitationRepo.GetByIdAsync(Arg.Any<OrgInvitationId>(), Arg.Any<CancellationToken>())
            .Returns((OrgInvitation?)null);

        var command = new AcceptOrgInvitationCommand(new OrgInvitationId(42).ToString());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrgErrors.OrgInvitation.NotFound);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
