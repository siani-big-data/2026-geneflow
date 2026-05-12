using GeneFlow.ApiNet2.Application.Identity.Commands.OAuthLogin;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for OAuthLoginCommandHandler.
/// </summary>
public class OAuthLoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly IOAuthTokenValidator _oAuthValidator = Substitute.For<IOAuthTokenValidator>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly ISequenceGenerator _sequenceGenerator = Substitute.For<ISequenceGenerator>();
    private readonly ILogger<OAuthLoginCommandHandler> _logger = Substitute.For<ILogger<OAuthLoginCommandHandler>>();
    private readonly OAuthLoginCommandHandler _handler;

    public OAuthLoginCommandHandlerTests()
    {
        _handler = new OAuthLoginCommandHandler(
            _userRepository,
            _unitOfWork,
            _oAuthValidator,
            _tokenGenerator,
            _sequenceGenerator,
            _logger);
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

    private static User CreateOAuthUser(long id = 1)
    {
        var userId = new UserId(id);
        var email = Email.Create("oauth@example.com").Value;
        var username = Username.Create("oauthuser").Value;

        return User.CreateFromOAuth(userId, email, username, ExternalProvider.Google, "google_123").Value;
    }

    private static OAuthUserInfo CreateOAuthUserInfo(string email = "oauth@example.com", string providerKey = "google_123")
    {
        return new OAuthUserInfo
        {
            ProviderKey = providerKey,
            Email = email,
            DisplayName = "OAuth User"
        };
    }

    private void SetupTokenGenerator()
    {
        _tokenGenerator
            .GenerateAccessToken(Arg.Any<User>())
            .Returns(("access_token", DateTime.UtcNow.AddMinutes(15)));

        _tokenGenerator
            .GenerateRefreshToken()
            .Returns(("refresh_token", DateTime.UtcNow.AddDays(7)));
    }

    #endregion

    #region Success Cases - Existing User with OAuth

    [Fact]
    public async Task Handle_WithExistingOAuthUser_ShouldReturnLoginResult()
    {
        // Arrange
        var user = CreateOAuthUser();
        var command = new OAuthLoginCommand("Google", "valid_google_token");
        var oAuthUserInfo = CreateOAuthUserInfo();

        _oAuthValidator
            .ValidateTokenAsync(ExternalProvider.Google, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result.Success(oAuthUserInfo));

        _userRepository
            .GetByExternalLoginAsync(ExternalProvider.Google, oAuthUserInfo.ProviderKey, Arg.Any<CancellationToken>())
            .Returns(user);

        SetupTokenGenerator();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.User.Should().NotBeNull();
        result.Value.Tokens.Should().NotBeNull();
        result.Value.Tokens.AccessToken.Should().Be("access_token");
        result.Value.RequiresTwoFactor.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithExistingOAuthUser_ShouldRecordSuccessfulLogin()
    {
        // Arrange
        var user = CreateOAuthUser();
        var command = new OAuthLoginCommand("Google", "valid_token");
        var oAuthUserInfo = CreateOAuthUserInfo();

        _oAuthValidator
            .ValidateTokenAsync(ExternalProvider.Google, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result.Success(oAuthUserInfo));

        _userRepository
            .GetByExternalLoginAsync(ExternalProvider.Google, oAuthUserInfo.ProviderKey, Arg.Any<CancellationToken>())
            .Returns(user);

        SetupTokenGenerator();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Success Cases - Create New User

    [Fact]
    public async Task Handle_WithNewOAuthUser_ShouldCreateUserAndReturnLoginResult()
    {
        // Arrange
        var command = new OAuthLoginCommand("Google", "valid_token");
        var oAuthUserInfo = CreateOAuthUserInfo("newuser@example.com", "new_google_key");
        var email = Email.Create("newuser@example.com").Value;

        _oAuthValidator
            .ValidateTokenAsync(ExternalProvider.Google, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result.Success(oAuthUserInfo));

        _userRepository
            .GetByExternalLoginAsync(ExternalProvider.Google, oAuthUserInfo.ProviderKey, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        _userRepository
            .GetByEmailAsync(email, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        _userRepository
            .ExistsWithUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _sequenceGenerator.NextAsync("user", Arg.Any<CancellationToken>()).Returns(1L);

        SetupTokenGenerator();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.User.Should().NotBeNull();
        result.Value.Tokens.Should().NotBeNull();
        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNewOAuthUser_ShouldGenerateUniqueUsername()
    {
        // Arrange
        var command = new OAuthLoginCommand("GitHub", "valid_token");
        var oAuthUserInfo = new OAuthUserInfo
        {
            ProviderKey = "github_123",
            Email = "developer@example.com",
            DisplayName = "John Developer"
        };

        _oAuthValidator
            .ValidateTokenAsync(ExternalProvider.GitHub, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result.Success(oAuthUserInfo));

        _userRepository
            .GetByExternalLoginAsync(Arg.Any<ExternalProvider>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        _userRepository
            .GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // First username exists, second doesn't
        _userRepository
            .ExistsWithUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>())
            .Returns(true, false);

        _sequenceGenerator.NextAsync("user", Arg.Any<CancellationToken>()).Returns(1L);
        SetupTokenGenerator();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Success Cases - Link to Existing Account by Email

    [Fact]
    public async Task Handle_WithExistingEmailUser_ShouldLinkOAuthAndReturnLoginResult()
    {
        // Arrange
        var existingUser = CreateVerifiedUser();
        var command = new OAuthLoginCommand("Google", "valid_token");
        var oAuthUserInfo = new OAuthUserInfo
        {
            ProviderKey = "google_new_key",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        _oAuthValidator
            .ValidateTokenAsync(ExternalProvider.Google, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result.Success(oAuthUserInfo));

        _userRepository
            .GetByExternalLoginAsync(ExternalProvider.Google, oAuthUserInfo.ProviderKey, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        _userRepository
            .GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        SetupTokenGenerator();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingUser.ExternalLogins.Should().Contain(e => e.Provider == ExternalProvider.Google);
    }

    #endregion

    #region Failure Cases - Invalid Provider

    [Fact]
    public async Task Handle_WithInvalidProvider_ShouldReturnFailure()
    {
        // Arrange
        var command = new OAuthLoginCommand("InvalidProvider", "valid_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ProviderNotSupported");
    }

    #endregion

    #region Failure Cases - Invalid Token

    [Fact]
    public async Task Handle_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var command = new OAuthLoginCommand("Google", "invalid_token");

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

    #region Failure Cases - Deactivated/Deleted User

    [Fact]
    public async Task Handle_WithDeactivatedUser_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateOAuthUser();
        user.Deactivate();
        var command = new OAuthLoginCommand("Google", "valid_token");
        var oAuthUserInfo = CreateOAuthUserInfo();

        _oAuthValidator
            .ValidateTokenAsync(ExternalProvider.Google, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result.Success(oAuthUserInfo));

        _userRepository
            .GetByExternalLoginAsync(ExternalProvider.Google, oAuthUserInfo.ProviderKey, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Deactivated");
    }

    [Fact]
    public async Task Handle_WithGitHubProvider_ShouldReturnLoginResult()
    {
        // Arrange
        var user = CreateOAuthUser();
        user.LinkExternalLogin(ExternalProvider.GitHub, "github_123");
        var command = new OAuthLoginCommand("GitHub", "valid_github_token");
        var oAuthUserInfo = new OAuthUserInfo
        {
            ProviderKey = "github_123",
            Email = "oauth@example.com",
            DisplayName = "GitHub User"
        };

        _oAuthValidator
            .ValidateTokenAsync(ExternalProvider.GitHub, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result.Success(oAuthUserInfo));

        _userRepository
            .GetByExternalLoginAsync(ExternalProvider.GitHub, oAuthUserInfo.ProviderKey, Arg.Any<CancellationToken>())
            .Returns(user);

        SetupTokenGenerator();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}
