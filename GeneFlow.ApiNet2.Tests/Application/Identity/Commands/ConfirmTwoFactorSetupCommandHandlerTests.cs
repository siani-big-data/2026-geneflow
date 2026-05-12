using GeneFlow.ApiNet2.Application.Identity.Commands.ConfirmTwoFactorSetup;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for ConfirmTwoFactorSetupCommandHandler.
/// </summary>
public class ConfirmTwoFactorSetupCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly ITwoFactorAuthenticator _twoFactorAuthenticator = Substitute.For<ITwoFactorAuthenticator>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly ConfirmTwoFactorSetupCommandHandler _handler;

    public ConfirmTwoFactorSetupCommandHandlerTests()
    {
        _handler = new ConfirmTwoFactorSetupCommandHandler(
            _userRepository,
            _unitOfWork,
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
    public async Task Handle_WithValidCode_ShouldEnableTwoFactor()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ConfirmTwoFactorSetupCommand(user.Id.ToString(), "123456");
        var secret = "JBSWY3DPEHPK3PXP";
        var encryptedSecret = "encrypted_secret";

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _cacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(secret);
        _twoFactorAuthenticator.ValidateCode(secret, command.Code).Returns(true);
        _twoFactorAuthenticator.EncryptSecret(secret).Returns(encryptedSecret);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeTrue();
        user.IsTotpConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidCode_ShouldRemoveSecretFromCache()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ConfirmTwoFactorSetupCommand(user.Id.ToString(), "123456");
        var secret = "JBSWY3DPEHPK3PXP";

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _cacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(secret);
        _twoFactorAuthenticator.ValidateCode(secret, command.Code).Returns(true);
        _twoFactorAuthenticator.EncryptSecret(secret).Returns("encrypted");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _cacheService.Received(1).RemoveAsync(
            Arg.Is<string>(k => k.Contains(user.Id.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCode_ShouldEncryptSecretBeforeStoring()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ConfirmTwoFactorSetupCommand(user.Id.ToString(), "123456");
        var secret = "JBSWY3DPEHPK3PXP";

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _cacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(secret);
        _twoFactorAuthenticator.ValidateCode(secret, command.Code).Returns(true);
        _twoFactorAuthenticator.EncryptSecret(secret).Returns("encrypted_secret_value");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _twoFactorAuthenticator.Received(1).EncryptSecret(secret);
    }

    #endregion

    #region Failure Cases - Invalid User

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new ConfirmTwoFactorSetupCommand("invalid_user_id", "123456");

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
        var command = new ConfirmTwoFactorSetupCommand(userId.ToString(), "123456");

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Failure Cases - Already Enabled

    [Fact]
    public async Task Handle_WithTwoFactorAlreadyEnabled_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUser();
        user.EnableTotpTwoFactor("encrypted_secret");
        var command = new ConfirmTwoFactorSetupCommand(user.Id.ToString(), "123456");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TwoFactorAlreadyEnabled");
    }

    #endregion

    #region Failure Cases - Expired/Missing Secret

    [Fact]
    public async Task Handle_WithExpiredSecret_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ConfirmTwoFactorSetupCommand(user.Id.ToString(), "123456");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _cacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TwoFactorSetupExpired");
    }

    [Fact]
    public async Task Handle_WithEmptySecret_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ConfirmTwoFactorSetupCommand(user.Id.ToString(), "123456");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _cacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(string.Empty);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TwoFactorSetupExpired");
    }

    #endregion

    #region Failure Cases - Invalid Code

    [Fact]
    public async Task Handle_WithInvalidCode_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ConfirmTwoFactorSetupCommand(user.Id.ToString(), "000000");
        var secret = "JBSWY3DPEHPK3PXP";

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _cacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(secret);
        _twoFactorAuthenticator.ValidateCode(secret, command.Code).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTwoFactorCode");
    }

    #endregion
}
