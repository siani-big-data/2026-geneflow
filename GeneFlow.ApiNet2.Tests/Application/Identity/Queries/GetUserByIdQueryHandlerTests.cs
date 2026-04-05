using GeneFlow.ApiNet2.Application.Identity.Queries.GetUserById;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Queries;

/// <summary>
/// Unit tests for GetUserByIdQueryHandler.
/// </summary>
public class GetUserByIdQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _handler = new GetUserByIdQueryHandler(_userRepository);
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
    public async Task Handle_WithValidId_ShouldReturnUserDto()
    {
        // Arrange
        var userId = new UserId(1);
        var user = CreateTestUser(userId);

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetUserByIdQuery("U00000001");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("U00000001");
        result.Value.Email.Should().Be(user.Email.Value);
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidIdFormat_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUserByIdQuery("invalid_id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var query = new GetUserByIdQuery("U00000999");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldQueryRepositoryWithParsedUserId()
    {
        // Arrange
        var userId = new UserId(42);
        var user = CreateTestUser(userId);

        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var query = new GetUserByIdQuery("U00000042");

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _userRepository.Received(1).GetByIdAsync(
            Arg.Is<UserId>(id => id.ToString() == "U00000042"),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
