using GeneFlow.ApiNet2.Application.Identity.Commands.DisableTwoFactor;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for DisableTwoFactorCommandHandler.
/// </summary>
public class DisableTwoFactorCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly DisableTwoFactorCommandHandler _handler;

    public DisableTwoFactorCommandHandlerTests()
    {
        _handler = new DisableTwoFactorCommandHandler(
            _userRepository,
            _unitOfWork);
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

    private static User CreateUserWithTwoFactorEnabled(long id = 1)
    {
        var user = CreateVerifiedUser(id);
        user.EnableTotpTwoFactor("encrypted_secret");
        return user;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithTwoFactorEnabled_ShouldDisableTwoFactor()
    {
        // Arrange
        var user = CreateUserWithTwoFactorEnabled();
        var command = new DisableTwoFactorCommand(user.Id.ToString());

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithTwoFactorEnabled_ShouldCallSaveChanges()
    {
        // Arrange
        var user = CreateUserWithTwoFactorEnabled();
        var command = new DisableTwoFactorCommand(user.Id.ToString());

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTwoFactorEnabled_ShouldClearTotpSecret()
    {
        // Arrange
        var user = CreateUserWithTwoFactorEnabled();
        var command = new DisableTwoFactorCommand(user.Id.ToString());

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsTotpConfigured.Should().BeFalse();
    }

    #endregion

    #region Failure Cases - Invalid User

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new DisableTwoFactorCommand("invalid_user_id");

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
        var command = new DisableTwoFactorCommand(userId.ToString());

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Failure Cases - Two-Factor Not Enabled

    [Fact]
    public async Task Handle_WithTwoFactorNotEnabled_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new DisableTwoFactorCommand(user.Id.ToString());

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TwoFactorNotEnabled");
    }

    #endregion

    #region Interaction Verification

    [Fact]
    public async Task Handle_ShouldCallUserRepository()
    {
        // Arrange
        var user = CreateUserWithTwoFactorEnabled();
        var command = new DisableTwoFactorCommand(user.Id.ToString());

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _userRepository.Received(1).GetByIdAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDisableFails_ShouldNotSaveChanges()
    {
        // Arrange
        var user = CreateVerifiedUser(); // 2FA not enabled
        var command = new DisableTwoFactorCommand(user.Id.ToString());

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
