using GeneFlow.ApiNet2.Application.Identity.Commands.RequestPasswordReset;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for RequestPasswordResetCommandHandler.
/// </summary>
public class RequestPasswordResetCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly RequestPasswordResetCommandHandler _handler;

    public RequestPasswordResetCommandHandlerTests()
    {
        _handler = new RequestPasswordResetCommandHandler(
            _userRepository,
            _unitOfWork);
    }

    #region Helper Methods

    private static User CreateVerifiedUser(long id = 1, string email = "test@example.com")
    {
        var userId = new UserId(id);
        var emailValue = Email.Create(email).Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hashedpassword").Value;

        var user = User.Create(userId, emailValue, username, passwordHash).Value;
        user.VerifyEmail(user.EmailVerificationToken!);
        return user;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new RequestPasswordResetCommand("test@example.com");

        _userRepository.GetByEmailStringAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidEmail_ShouldGenerateResetToken()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new RequestPasswordResetCommand("test@example.com");

        _userRepository.GetByEmailStringAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.PasswordResetToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithValidEmail_ShouldCallSaveChanges()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new RequestPasswordResetCommand("test@example.com");

        _userRepository.GetByEmailStringAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Security Cases - Non-Existent Email

    [Fact]
    public async Task Handle_WithNonExistentEmail_ShouldReturnSuccess()
    {
        // Arrange
        // Security: Don't reveal whether email exists
        var command = new RequestPasswordResetCommand("nonexistent@example.com");

        _userRepository.GetByEmailStringAsync(command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Should succeed to prevent email enumeration attacks
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentEmail_ShouldNotCallSaveChanges()
    {
        // Arrange
        var command = new RequestPasswordResetCommand("nonexistent@example.com");

        _userRepository.GetByEmailStringAsync(command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Interaction Verification

    [Fact]
    public async Task Handle_ShouldCallUserRepository()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new RequestPasswordResetCommand("test@example.com");

        _userRepository.GetByEmailStringAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _userRepository.Received(1).GetByEmailStringAsync(command.Email, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidEmail_ShouldCallRequestPasswordResetOnUser()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var initialToken = user.PasswordResetToken;
        var command = new RequestPasswordResetCommand("test@example.com");

        _userRepository.GetByEmailStringAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Token should be generated (different from initial null)
        user.PasswordResetToken.Should().NotBe(initialToken);
    }

    #endregion

    #region Multiple Requests

    [Fact]
    public async Task Handle_WithMultipleRequests_ShouldGenerateNewTokenEachTime()
    {
        // Arrange
        var user = CreateVerifiedUser();
        var command = new RequestPasswordResetCommand("test@example.com");

        _userRepository.GetByEmailStringAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);
        var firstToken = user.PasswordResetToken;

        await _handler.Handle(command, CancellationToken.None);
        var secondToken = user.PasswordResetToken;

        // Assert
        firstToken.Should().NotBeNullOrEmpty();
        secondToken.Should().NotBeNullOrEmpty();
        secondToken.Should().NotBe(firstToken);
    }

    #endregion
}
