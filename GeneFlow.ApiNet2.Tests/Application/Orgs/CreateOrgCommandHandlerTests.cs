using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Commands.CreateOrg;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs;
using OrgAggregate = GeneFlow.ApiNet2.Domain.Orgs.Org;

namespace GeneFlow.ApiNet2.Tests.Application.OrgsTests;

public class CreateOrgCommandHandlerTests
{
    private readonly IOrgRepository _orgRepo = Substitute.For<IOrgRepository>();
    private readonly IOrgUnitOfWork _uow = Substitute.For<IOrgUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly CreateOrgCommandHandler _handler;

    public CreateOrgCommandHandlerTests()
    {
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _orgRepo.GetNextSequenceValueAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1L));
        _currentUser.UserId.Returns(new UserId(42));

        _handler = new CreateOrgCommandHandler(_orgRepo, _uow, _currentUser);
    }

    [Fact]
    public async Task Handle_HappyPath_CreatesOrgAndReturnsDto()
    {
        _orgRepo.HandleExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var command = new CreateOrgCommand("acme", "Acme Inc", "Description", "Public");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Handle.Should().Be("acme");
        result.Value.Name.Should().Be("Acme Inc");
        result.Value.Description.Should().Be("Description");
        result.Value.Visibility.Should().Be("Public");
        await _orgRepo.Received(1).AddAsync(Arg.Any<OrgAggregate>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HandleAlreadyTaken_ReturnsFailure()
    {
        _orgRepo.HandleExistsAsync("acme", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var command = new CreateOrgCommand("acme", "Acme Inc", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrgErrors.HandleTaken);
        await _orgRepo.DidNotReceive().AddAsync(Arg.Any<OrgAggregate>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
