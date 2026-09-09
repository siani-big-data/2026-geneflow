using GeneFlow.ApiNet2.Application.Identity.Commands.UnlinkExternalLogin;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for UnlinkExternalLoginCommandHandler.
/// </summary>
public class UnlinkExternalLoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly UnlinkExternalLoginCommandHandler _handler;

    public UnlinkExternalLoginCommandHandlerTests()
    {
        _handler = new UnlinkExternalLoginCommandHandler(
            _userRepository,
            _unitOfWork);
    }

    #region Helper Methods

    private static User CreateVerifiedUserWithPassword(long id = 1)
    {
        var userId = new UserId(id);
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hashedpassword").Value;

        var user = User.Create(userId, email, username, passwordHash).Value;
        user.VerifyEmail(user.EmailVerificationToken!);
        return user;
    }

    private static User CreateOAuthOnlyUser(long id = 1)
    {
        var userId = new UserId(id);
        var email = Email.Create("oauth@example.com").Value;
        var username = Username.Create("oauthuser").Value;

        return User.CreateFromOAuth(userId, email, username, ExternalProvider.Google, "google_123").Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithPasswordAndOAuth_ShouldUnlinkSuccessfully()
    {
        // Arrange
        var user = CreateVerifiedUserWithPassword();
        user.LinkExternalLogin(ExternalProvider.Google, "google_123");
        var command = new UnlinkExternalLoginCommand(user.Id.ToString(), "Google");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.ExternalLogins.Should().NotContain(e => e.Provider == ExternalProvider.Google);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMultipleOAuthProviders_ShouldUnlinkOne()
    {
        // Arrange
        var user = CreateOAuthOnlyUser();
        user.LinkExternalLogin(ExternalProvider.GitHub, "github_456");
        var command = new UnlinkExternalLoginCommand(user.Id.ToString(), "Google");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.ExternalLogins.Should().NotContain(e => e.Provider == ExternalProvider.Google);
        user.ExternalLogins.Should().Contain(e => e.Provider == ExternalProvider.GitHub);
    }

    #endregion

    #region Failure Cases - Invalid User

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new UnlinkExternalLoginCommand("invalid_user_id", "Google");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = new UserId(999);
        var command = new UnlinkExternalLoginCommand(userId.ToString(), "Google");

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Failure Cases - Invalid Provider

    [Fact]
    public async Task Handle_WithInvalidProvider_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUserWithPassword();
        var command = new UnlinkExternalLoginCommand(user.Id.ToString(), "InvalidProvider");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ProviderNotSupported");
    }

    #endregion

    #region Failure Cases - Cannot Unlink Only Auth Method

    [Fact]
    public async Task Handle_WithOnlyOAuthAndNoPassword_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateOAuthOnlyUser();
        var command = new UnlinkExternalLoginCommand(user.Id.ToString(), "Google");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotUnlinkOnlyAuthMethod");
    }

    #endregion
}
