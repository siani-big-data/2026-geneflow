using GeneFlow.ApiNet2.Application.Identity.Commands.LinkExternalLogin;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for LinkExternalLoginCommandHandler.
/// </summary>
public class LinkExternalLoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly IOAuthTokenValidator _oAuthValidator = Substitute.For<IOAuthTokenValidator>();
    private readonly LinkExternalLoginCommandHandler _handler;

    public LinkExternalLoginCommandHandlerTests()
    {
        _handler = new LinkExternalLoginCommandHandler(
            _userRepository,
            _unitOfWork,
            _oAuthValidator);
    }

    #region Helper Methods

    private static User CreateVerifiedUser(long id = 1)
    {
        var userId = new UserId(id);
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hashedpassword").Value;

        var user = User.Create(userId, email, username, passwordHash).Value;
        user.VerifyEmail(user.EmailVerificationToken!);
        return user;
    }

    private static OAuthUserInfo CreateOAuthUserInfo(string providerKey = "google_123")
    {
        return new OAuthUserInfo
        {
            ProviderKey = providerKey,
            Email = "oauth@example.com",
            DisplayName = "OAuth User"
        };
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidRequest_ShouldLinkExternalLogin()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new LinkExternalLoginCommand(user.Id.ToString(), "Google", "valid_token");
        var oAuthUserInfo = CreateOAuthUserInfo("new_google_key");

        _userRepository
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        _oAuthValidator
            .ValidateTokenAsync(ExternalProvider.Google, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result.Success(oAuthUserInfo));

        _userRepository
            .GetByExternalLoginAsync(ExternalProvider.Google, oAuthUserInfo.ProviderKey, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.ExternalLogins.Should().Contain(e => e.Provider == ExternalProvider.Google);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithGitHubProvider_ShouldLinkSuccessfully()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new LinkExternalLoginCommand(user.Id.ToString(), "GitHub", "valid_token");
        var oAuthUserInfo = new OAuthUserInfo
        {
            ProviderKey = "github_456",
            Email = "github@example.com",
            DisplayName = "GitHub User"
        };

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _oAuthValidator
            .ValidateTokenAsync(ExternalProvider.GitHub, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result.Success(oAuthUserInfo));
        _userRepository
            .GetByExternalLoginAsync(ExternalProvider.GitHub, oAuthUserInfo.ProviderKey, Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.ExternalLogins.Should().Contain(e => e.Provider == ExternalProvider.GitHub);
    }

    #endregion

    #region Failure Cases - Invalid User

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new LinkExternalLoginCommand("invalid_user_id", "Google", "valid_token");

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
        var command = new LinkExternalLoginCommand(userId.ToString(), "Google", "valid_token");

        _userRepository
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Failure Cases - Invalid Provider/Token

    [Fact]
    public async Task Handle_WithInvalidProvider_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new LinkExternalLoginCommand(user.Id.ToString(), "InvalidProvider", "valid_token");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ProviderNotSupported");
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new LinkExternalLoginCommand(user.Id.ToString(), "Google", "invalid_token");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _oAuthValidator
            .ValidateTokenAsync(ExternalProvider.Google, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<OAuthUserInfo>(OAuthErrors.InvalidToken));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidToken");
    }

    #endregion

    #region Failure Cases - Already Linked

    [Fact]
    public async Task Handle_WithOAuthAlreadyLinkedToAnotherUser_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUser(1);
        var otherUser = CreateVerifiedUser(2);
        var command = new LinkExternalLoginCommand(user.Id.ToString(), "Google", "valid_token");
        var oAuthUserInfo = CreateOAuthUserInfo("shared_google_key");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _oAuthValidator
            .ValidateTokenAsync(ExternalProvider.Google, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result.Success(oAuthUserInfo));
        _userRepository
            .GetByExternalLoginAsync(ExternalProvider.Google, oAuthUserInfo.ProviderKey, Arg.Any<CancellationToken>())
            .Returns(otherUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ProviderAlreadyLinkedToAnotherAccount");
    }

    #endregion
}
