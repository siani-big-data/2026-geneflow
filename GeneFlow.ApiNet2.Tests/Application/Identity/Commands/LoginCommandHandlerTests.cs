using GeneFlow.ApiNet2.Application.Identity.Commands.Login;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for LoginCommandHandler.
/// </summary>
public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IUserAuthenticationValidator _authValidator = Substitute.For<IUserAuthenticationValidator>();
    private readonly ITwoFactorAuthenticator _twoFactorAuthenticator = Substitute.For<ITwoFactorAuthenticator>();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _userRepository,
            _unitOfWork,
            _passwordHasher,
            _tokenGenerator,
            _authValidator,
            _twoFactorAuthenticator);
    }

    #region Helper Methods

    private static User CreateVerifiedUser()
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
    public async Task Handle_WithValidCredentials_ShouldReturnLoginResult()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new LoginCommand("test@example.com", "Password123!", null);

        _userRepository
            .GetByEmailOrUsernameAsync(command.EmailOrUsername, Arg.Any<CancellationToken>())
            .Returns(user);

        _authValidator
            .ValidateCanAuthenticate(Arg.Any<User>())
            .Returns(Result.Success());

        _authValidator
            .ValidatePassword(Arg.Any<User>(), command.Password)
            .Returns(Result.Success());

        _tokenGenerator
            .GenerateAccessToken(Arg.Any<User>())
            .Returns(("access_token", DateTime.UtcNow.AddMinutes(15)));

        _tokenGenerator
            .GenerateRefreshToken()
            .Returns(("refresh_token", DateTime.UtcNow.AddDays(7)));

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

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
    public async Task Handle_WithValidCredentials_ShouldGenerateTokens()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new LoginCommand("test@example.com", "Password123!", null);

        _userRepository.GetByEmailOrUsernameAsync(command.EmailOrUsername, Arg.Any<CancellationToken>()).Returns(user);
        _authValidator.ValidateCanAuthenticate(Arg.Any<User>()).Returns(Result.Success());
        _authValidator.ValidatePassword(Arg.Any<User>(), command.Password).Returns(Result.Success());
        _tokenGenerator.GenerateAccessToken(Arg.Any<User>()).Returns(("token", DateTime.UtcNow.AddMinutes(15)));
        _tokenGenerator.GenerateRefreshToken().Returns(("refresh", DateTime.UtcNow.AddDays(7)));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenGenerator.Received(1).GenerateAccessToken(Arg.Any<User>());
        _tokenGenerator.Received(1).GenerateRefreshToken();
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "Password123!", null);

        _userRepository
            .GetByEmailOrUsernameAsync(command.EmailOrUsername, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidCredentials");
    }

    [Fact]
    public async Task Handle_WithDeactivatedUser_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new LoginCommand("test@example.com", "Password123!", null);

        _userRepository.GetByEmailOrUsernameAsync(command.EmailOrUsername, Arg.Any<CancellationToken>()).Returns(user);
        _authValidator.ValidateCanAuthenticate(Arg.Any<User>()).Returns(Result.Failure(UserErrors.UserDeactivated));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new LoginCommand("test@example.com", "WrongPassword!", null);

        _userRepository.GetByEmailOrUsernameAsync(command.EmailOrUsername, Arg.Any<CancellationToken>()).Returns(user);
        _authValidator.ValidateCanAuthenticate(Arg.Any<User>()).Returns(Result.Success());
        _authValidator.ValidatePassword(Arg.Any<User>(), command.Password).Returns(Result.Failure(UserErrors.InvalidCredentials));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidCredentials");
    }

    #endregion

    #region Two-Factor Authentication

    [Fact]
    public async Task Handle_With2FAEnabled_NoCode_ShouldRequireTwoFactor()
    {
        // Arrange
        var user = CreateVerifiedUser();
        user.EnableTwoFactor();
        var command = new LoginCommand("test@example.com", "Password123!", null);

        _userRepository.GetByEmailOrUsernameAsync(command.EmailOrUsername, Arg.Any<CancellationToken>()).Returns(user);
        _authValidator.ValidateCanAuthenticate(Arg.Any<User>()).Returns(Result.Success());
        _authValidator.ValidatePassword(Arg.Any<User>(), command.Password).Returns(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RequiresTwoFactor.Should().BeTrue();
        result.Value.Tokens.Should().BeNull();
    }

    #endregion
}
