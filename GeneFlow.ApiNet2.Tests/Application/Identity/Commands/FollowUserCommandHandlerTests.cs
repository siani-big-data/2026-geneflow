using GeneFlow.ApiNet2.Application.Identity.Commands.FollowUser;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Entities;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for FollowUserCommandHandler.
/// </summary>
public class FollowUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly IDomainEventDispatcher _eventDispatcher = Substitute.For<IDomainEventDispatcher>();
    private readonly FollowUserCommandHandler _handler;

    public FollowUserCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new FollowUserCommandHandler(
            _userRepository,
            _unitOfWork,
            _eventDispatcher);
    }

    private static User CreateUser(int id)
    {
        var email = Email.Create($"user{id}@example.com").Value;
        var username = Username.Create($"user{id}").Value;
        return User.CreateFromOAuth(
            new UserId(id), email, username, ExternalProvider.Google, $"ext-{id}").Value;
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldFollowUser()
    {
        var command = new FollowUserCommand("U00000001", "U00000002");
        var followee = CreateUser(2);

        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(followee);
        _userRepository.IsFollowingAsync(Arg.Any<UserId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _userRepository.Received(1).AddFollowAsync(Arg.Any<UserFollow>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _eventDispatcher.Received(1).DispatchAsync(
            Arg.Any<IDomainEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FollowSelf_ShouldFail()
    {
        var command = new FollowUserCommand("U00000001", "U00000001");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.CannotFollowSelf.Code);
        await _userRepository.DidNotReceive().AddFollowAsync(Arg.Any<UserFollow>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidFollowerId_ShouldFail()
    {
        var command = new FollowUserCommand("invalid", "U00000002");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.InvalidUserId.Code);
    }

    [Fact]
    public async Task Handle_InvalidFolloweeId_ShouldFail()
    {
        var command = new FollowUserCommand("U00000001", "invalid");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.InvalidUserId.Code);
    }

    [Fact]
    public async Task Handle_FolloweeNotFound_ShouldFail()
    {
        var command = new FollowUserCommand("U00000001", "U00000002");

        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _userRepository.DidNotReceive().AddFollowAsync(Arg.Any<UserFollow>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyFollowing_ShouldFail()
    {
        var command = new FollowUserCommand("U00000001", "U00000002");
        var followee = CreateUser(2);

        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(followee);
        _userRepository.IsFollowingAsync(Arg.Any<UserId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.AlreadyFollowing.Code);
        await _userRepository.DidNotReceive().AddFollowAsync(Arg.Any<UserFollow>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
