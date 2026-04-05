using GeneFlow.ApiNet2.Application.Identity.Commands.RefreshToken;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using RefreshTokenVO = GeneFlow.ApiNet2.Domain.Identity.ValueObjects.RefreshToken;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for RefreshTokenCommandHandler.
/// </summary>
public class RefreshTokenCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _userRepository,
            _unitOfWork,
            _tokenGenerator);
    }

    #region Helper Methods

    private static User CreateActiveUser()
    {
        var userId = new UserId(1);
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hashedpassword").Value;

        var user = User.Create(userId, email, username, passwordHash).Value;
        user.VerifyEmail(user.EmailVerificationToken!);
        return user;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var user = CreateActiveUser();
        var existingToken = RefreshTokenVO.Create("valid_refresh_token", DateTime.UtcNow.AddDays(7)).Value;
        user.AddRefreshToken(existingToken);

        var command = new RefreshTokenCommand("valid_refresh_token");

        _userRepository
            .GetByRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(user);

        _tokenGenerator
            .GenerateAccessToken(Arg.Any<User>())
            .Returns(("new_access_token", DateTime.UtcNow.AddMinutes(15)));

        _tokenGenerator
            .GenerateRefreshToken()
            .Returns(("new_refresh_token", DateTime.UtcNow.AddDays(7)));

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new_access_token");
        result.Value.RefreshToken.Should().Be("new_refresh_token");
    }

    [Fact]
    public async Task Handle_ShouldRotateTokens()
    {
        // Arrange
        var user = CreateActiveUser();
        var existingToken = RefreshTokenVO.Create("valid_refresh_token", DateTime.UtcNow.AddDays(7)).Value;
        user.AddRefreshToken(existingToken);

        var command = new RefreshTokenCommand("valid_refresh_token");

        _userRepository.GetByRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>()).Returns(user);
        _tokenGenerator.GenerateAccessToken(Arg.Any<User>()).Returns(("token", DateTime.UtcNow.AddMinutes(15)));
        _tokenGenerator.GenerateRefreshToken().Returns(("new_token", DateTime.UtcNow.AddDays(7)));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Old token should be removed (AddRefreshToken cleans up inactive tokens)
        // and new token should be added
        var oldToken = user.GetRefreshToken("valid_refresh_token");
        oldToken.Should().BeNull("old token is removed when AddRefreshToken cleans up inactive tokens");

        var newToken = user.GetRefreshToken("new_token");
        newToken.Should().NotBeNull();
        newToken!.IsActive.Should().BeTrue();
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithNonExistentToken_ShouldReturnFailure()
    {
        // Arrange
        var command = new RefreshTokenCommand("nonexistent_token");

        _userRepository
            .GetByRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("RefreshTokenNotFound");
    }

    [Fact]
    public async Task Handle_WithRevokedToken_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateActiveUser();
        var token = RefreshTokenVO.Create("revoked_token", DateTime.UtcNow.AddDays(7)).Value;
        user.AddRefreshToken(token);
        user.RevokeRefreshToken("revoked_token");

        var command = new RefreshTokenCommand("revoked_token");

        _userRepository
            .GetByRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("RefreshTokenRevoked");
    }

    [Fact]
    public async Task Handle_WithDeactivatedUser_ShouldReturnFailure()
    {
        // Arrange - Note: Deactivate() revokes all refresh tokens, so the error will be
        // RefreshTokenRevoked, not UserDeactivated. This is the correct behavior since
        // deactivation invalidates all tokens.
        var user = CreateActiveUser();
        var token = RefreshTokenVO.Create("valid_token", DateTime.UtcNow.AddDays(7)).Value;
        user.AddRefreshToken(token);
        user.Deactivate(); // This also revokes all refresh tokens

        var command = new RefreshTokenCommand("valid_token");

        _userRepository
            .GetByRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Token gets revoked when user is deactivated
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("RefreshTokenRevoked");
    }

    #endregion
}
