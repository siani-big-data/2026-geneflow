using GeneFlow.ApiNet2.Application.Identity.Commands.ResetPassword;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for ResetPasswordCommandHandler.
/// </summary>
public class ResetPasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _handler = new ResetPasswordCommandHandler(_userRepository, _unitOfWork, _passwordHasher);
    }

    #region Helper Methods

    private static User CreateUserWithResetToken()
    {
        var userId = new UserId(1);
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hashedpassword").Value;

        var user = User.Create(userId, email, username, passwordHash).Value;
        user.RequestPasswordReset();
        return user;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidToken_ShouldResetPassword()
    {
        // Arrange
        var user = CreateUserWithResetToken();
        var token = user.PasswordResetToken!;
        var command = new ResetPasswordCommand(token, "NewPassword123!");

        _userRepository
            .GetByPasswordResetTokenAsync(token, Arg.Any<CancellationToken>())
            .Returns(user);

        _passwordHasher
            .Hash(command.NewPassword)
            .Returns("$2a$12$newhashedpassword");

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PasswordResetToken.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldHashNewPassword()
    {
        // Arrange
        var user = CreateUserWithResetToken();
        var token = user.PasswordResetToken!;
        var command = new ResetPasswordCommand(token, "NewPassword123!");

        _userRepository.GetByPasswordResetTokenAsync(token, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Hash(command.NewPassword).Returns("$2a$12$hash");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.Received(1).Hash(command.NewPassword);
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithNonExistentToken_ShouldReturnFailure()
    {
        // Arrange
        var command = new ResetPasswordCommand("nonexistent_token", "NewPassword123!");

        _userRepository
            .GetByPasswordResetTokenAsync(command.Token, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateUserWithResetToken();
        var command = new ResetPasswordCommand("wrong_token", "NewPassword123!");

        _userRepository
            .GetByPasswordResetTokenAsync(command.Token, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion
}
