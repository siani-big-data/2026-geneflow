using GeneFlow.ApiNet2.Application.Identity.Commands.SetupTwoFactor;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for SetupTwoFactorCommandHandler.
/// </summary>
public class SetupTwoFactorCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITwoFactorAuthenticator _twoFactorAuthenticator = Substitute.For<ITwoFactorAuthenticator>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly SetupTwoFactorCommandHandler _handler;

    public SetupTwoFactorCommandHandlerTests()
    {
        _handler = new SetupTwoFactorCommandHandler(
            _userRepository,
            _twoFactorAuthenticator,
            _cacheService);
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

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidUser_ShouldReturnSetupDto()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new SetupTwoFactorCommand(user.Id.ToString());
        var secret = "JBSWY3DPEHPK3PXP";
        var qrCodeUri = "otpauth://totp/GeneFlow:test@example.com?secret=JBSWY3DPEHPK3PXP&issuer=GeneFlow";

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _twoFactorAuthenticator.GenerateSecret().Returns(secret);
        _twoFactorAuthenticator.GenerateQrCodeUri(user.Email.Value, secret, Arg.Any<string>()).Returns(qrCodeUri);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Secret.Should().Be(secret);
        result.Value.QrCodeUri.Should().Be(qrCodeUri);
    }

    [Fact]
    public async Task Handle_WithValidUser_ShouldCacheSecretForConfirmation()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new SetupTwoFactorCommand(user.Id.ToString());
        var secret = "JBSWY3DPEHPK3PXP";

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _twoFactorAuthenticator.GenerateSecret().Returns(secret);
        _twoFactorAuthenticator.GenerateQrCodeUri(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("otpauth://...");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _cacheService.Received(1).SetAsync(
            Arg.Is<string>(k => k.Contains(user.Id.ToString())),
            secret,
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldGenerateNewSecret()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new SetupTwoFactorCommand(user.Id.ToString());

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _twoFactorAuthenticator.GenerateSecret().Returns("SECRET123");
        _twoFactorAuthenticator.GenerateQrCodeUri(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("otpauth://...");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _twoFactorAuthenticator.Received(1).GenerateSecret();
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new SetupTwoFactorCommand("invalid_user_id");

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
        var command = new SetupTwoFactorCommand(userId.ToString());

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithTwoFactorAlreadyEnabled_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUser();
        user.EnableTotpTwoFactor("encrypted_secret");
        var command = new SetupTwoFactorCommand(user.Id.ToString());

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TwoFactorAlreadyEnabled");
    }

    #endregion
}
