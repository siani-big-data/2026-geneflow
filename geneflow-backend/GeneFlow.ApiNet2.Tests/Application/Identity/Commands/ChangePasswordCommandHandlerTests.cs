using GeneFlow.ApiNet2.Application.Identity.Commands.ChangePassword;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for ChangePasswordCommandHandler.
/// </summary>
public class ChangePasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _handler = new ChangePasswordCommandHandler(
            _userRepository,
            _unitOfWork,
            _passwordHasher);
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

    private static User CreateOAuthOnlyUser(long id = 1)
    {
        var userId = new UserId(id);
        var email = Email.Create("oauth@example.com").Value;
        var username = Username.Create("oauthuser").Value;
        // OAuth-only users have placeholder password hash
        var passwordHash = PasswordHash.CreatePlaceholder();

        var user = User.Create(userId, email, username, passwordHash).Value;
        user.VerifyEmail(user.EmailVerificationToken!);
        return user;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidCurrentPassword_ShouldChangePassword()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ChangePasswordCommand(user.Id, "OldPassword123!", "NewPassword456!");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.CurrentPassword, user.PasswordHash.Value).Returns(true);
        _passwordHasher.Hash(command.NewPassword).Returns("$2a$12$newhashedpassword");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidCurrentPassword_ShouldHashNewPassword()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ChangePasswordCommand(user.Id, "OldPassword123!", "NewPassword456!");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.CurrentPassword, user.PasswordHash.Value).Returns(true);
        _passwordHasher.Hash(command.NewPassword).Returns("$2a$12$newhashedpassword");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.Received(1).Hash(command.NewPassword);
    }

    [Fact]
    public async Task Handle_WithValidCurrentPassword_ShouldCallSaveChanges()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ChangePasswordCommand(user.Id, "OldPassword123!", "NewPassword456!");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.CurrentPassword, user.PasswordHash.Value).Returns(true);
        _passwordHasher.Hash(command.NewPassword).Returns("$2a$12$newhashedpassword");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCurrentPassword_ShouldUpdateUserPasswordHash()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var originalHash = user.PasswordHash.Value;
        var command = new ChangePasswordCommand(user.Id, "OldPassword123!", "NewPassword456!");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.CurrentPassword, originalHash).Returns(true);
        _passwordHasher.Hash(command.NewPassword).Returns("$2a$12$newhashedpassword");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.PasswordHash.Value.Should().NotBe(originalHash);
        user.PasswordHash.Value.Should().Be("$2a$12$newhashedpassword");
    }

    #endregion

    #region Failure Cases - Invalid User

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = new UserId(999);
        var command = new ChangePasswordCommand(userId, "OldPassword123!", "NewPassword456!");

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Failure Cases - Invalid Current Password

    [Fact]
    public async Task Handle_WithInvalidCurrentPassword_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ChangePasswordCommand(user.Id, "WrongPassword!", "NewPassword456!");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.CurrentPassword, user.PasswordHash.Value).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidCurrentPassword");
    }

    [Fact]
    public async Task Handle_WithInvalidCurrentPassword_ShouldNotHashNewPassword()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ChangePasswordCommand(user.Id, "WrongPassword!", "NewPassword456!");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.CurrentPassword, user.PasswordHash.Value).Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WithInvalidCurrentPassword_ShouldNotSaveChanges()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ChangePasswordCommand(user.Id, "WrongPassword!", "NewPassword456!");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.CurrentPassword, user.PasswordHash.Value).Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Failure Cases - Weak New Password

    [Theory]
    [InlineData("short")]           // Too short
    [InlineData("alllowercase1")]   // No uppercase
    [InlineData("ALLUPPERCASE1")]   // No lowercase
    [InlineData("NoDigitsHere!")]   // No digit
    public async Task Handle_WithWeakNewPassword_ShouldReturnFailure(string weakPassword)
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ChangePasswordCommand(user.Id, "OldPassword123!", weakPassword);

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.CurrentPassword, user.PasswordHash.Value).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Password");
    }

    [Fact]
    public async Task Handle_WithWeakNewPassword_ShouldNotHashPassword()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ChangePasswordCommand(user.Id, "OldPassword123!", "weak");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.CurrentPassword, user.PasswordHash.Value).Returns(true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
    }

    #endregion

    #region Failure Cases - OAuth-Only Account

    [Fact]
    public async Task Handle_WithOAuthOnlyAccount_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateOAuthOnlyUser();
        var command = new ChangePasswordCommand(user.Id, "AnyPassword123!", "NewPassword456!");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NoPasswordSet");
    }

    [Fact]
    public async Task Handle_WithOAuthOnlyAccount_ShouldNotVerifyPassword()
    {
        // Arrange
        var user = CreateOAuthOnlyUser();
        var command = new ChangePasswordCommand(user.Id, "AnyPassword123!", "NewPassword456!");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region Interaction Verification

    [Fact]
    public async Task Handle_ShouldCallUserRepository()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new ChangePasswordCommand(user.Id, "OldPassword123!", "NewPassword456!");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.CurrentPassword, user.PasswordHash.Value).Returns(true);
        _passwordHasher.Hash(command.NewPassword).Returns("$2a$12$newhashedpassword");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _userRepository.Received(1).GetByIdAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldVerifyCurrentPassword()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var originalPasswordHash = user.PasswordHash.Value;
        var command = new ChangePasswordCommand(user.Id, "OldPassword123!", "NewPassword456!");

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.CurrentPassword, originalPasswordHash).Returns(true);
        _passwordHasher.Hash(command.NewPassword).Returns("$2a$12$newhashedpassword");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.Received(1).Verify(command.CurrentPassword, originalPasswordHash);
    }

    #endregion
}
