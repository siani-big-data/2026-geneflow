using GeneFlow.ApiNet2.Application.Identity.Commands.VerifyEmail;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for VerifyEmailCommandHandler.
/// </summary>
public class VerifyEmailCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly VerifyEmailCommandHandler _handler;

    public VerifyEmailCommandHandlerTests()
    {
        _handler = new VerifyEmailCommandHandler(_userRepository, _unitOfWork);
    }

    #region Helper Methods

    private static User CreateUnverifiedUser()
    {
        var userId = new UserId(1);
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hashedpassword").Value;

        return User.Create(userId, email, username, passwordHash).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidToken_ShouldVerifyEmail()
    {
        // Arrange
        var user = CreateUnverifiedUser();
        var token = user.EmailVerificationToken!;
        var command = new VerifyEmailCommand(token);

        _userRepository
            .GetByEmailVerificationTokenAsync(token, Arg.Any<CancellationToken>())
            .Returns(user);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.EmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldSaveChanges()
    {
        // Arrange
        var user = CreateUnverifiedUser();
        var token = user.EmailVerificationToken!;
        var command = new VerifyEmailCommand(token);

        _userRepository.GetByEmailVerificationTokenAsync(token, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithNonExistentToken_ShouldReturnFailure()
    {
        // Arrange
        var command = new VerifyEmailCommand("nonexistent_token");

        _userRepository
            .GetByEmailVerificationTokenAsync(command.Token, Arg.Any<CancellationToken>())
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
        var user = CreateUnverifiedUser();
        var command = new VerifyEmailCommand("wrong_token");

        _userRepository
            .GetByEmailVerificationTokenAsync(command.Token, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion
}
