using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Identity.Queries.GetCurrentUser;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Queries;

/// <summary>
/// Unit tests for GetCurrentUserQueryHandler.
/// </summary>
public class GetCurrentUserQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _handler = new GetCurrentUserQueryHandler(_userRepository, _currentUserService);
    }

    #region Helper Methods

    private static User CreateTestUser(UserId id)
    {
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hashedpassword").Value;

        return User.Create(id, email, username, passwordHash).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithAuthenticatedUser_ShouldReturnUserDto()
    {
        // Arrange
        var userId = new UserId(1);
        var user = CreateTestUser(userId);

        _currentUserService.UserId.Returns(userId);
        _currentUserService.IsAuthenticated.Returns(true);

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(user.Email.Value);
        result.Value.Username.Should().Be(user.Username.Value);
    }

    [Fact]
    public async Task Handle_ShouldQueryRepositoryWithCorrectUserId()
    {
        // Arrange
        var userId = new UserId(42);
        var user = CreateTestUser(userId);

        _currentUserService.UserId.Returns(userId);
        _currentUserService.IsAuthenticated.Returns(true);

        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var query = new GetCurrentUserQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _userRepository.Received(1).GetByIdAsync(
            Arg.Is<UserId>(id => id.ToString() == "U00000042"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnFailure()
    {
        // Arrange
        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.UserId.Returns((UserId?)null);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        _currentUserService.UserId.Returns(new UserId(1));
        _currentUserService.IsAuthenticated.Returns(true);

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion
}
