using GeneFlow.ApiNet2.Application.Identity.Commands.Register;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Commands;

/// <summary>
/// Unit tests for RegisterUserCommandHandler.
/// </summary>
public class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ISequenceGenerator _sequenceGenerator = Substitute.For<ISequenceGenerator>();
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _handler = new RegisterUserCommandHandler(
            _userRepository,
            _unitOfWork,
            _passwordHasher,
            _sequenceGenerator);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "testuser", "Password123!");

        _sequenceGenerator
            .NextAsync(UserId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);

        _userRepository
            .ExistsWithEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _userRepository
            .ExistsWithUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _passwordHasher
            .Hash(command.Password)
            .Returns("$2a$12$hashedpassword");

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(command.Email);
        result.Value.Username.Should().Be(command.Username);

        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnUserDto()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "testuser", "Password123!");

        _sequenceGenerator
            .NextAsync(UserId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(42L);

        _userRepository
            .ExistsWithEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _userRepository
            .ExistsWithUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _passwordHasher
            .Hash(command.Password)
            .Returns("$2a$12$hashedpassword");

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("U00000042");
        result.Value.IsActive.Should().BeTrue();
        result.Value.EmailVerified.Should().BeFalse();
        result.Value.TwoFactorEnabled.Should().BeFalse();
    }

    #endregion

    #region Validation Failures

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public async Task Handle_WithInvalidEmail_ShouldReturnFailure(string email)
    {
        // Arrange
        var command = new RegisterUserCommand(email, "testuser", "Password123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public async Task Handle_WithInvalidUsername_ShouldReturnFailure(string username)
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", username, "Password123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Conflict Cases

    [Fact]
    public async Task Handle_WithExistingEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterUserCommand("existing@example.com", "newuser", "Password123!");

        _userRepository
            .ExistsWithEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("EmailAlreadyExists");
    }

    [Fact]
    public async Task Handle_WithExistingUsername_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterUserCommand("new@example.com", "existinguser", "Password123!");

        _userRepository
            .ExistsWithEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _userRepository
            .ExistsWithUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("UsernameAlreadyExists");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldGenerateSequenceId()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "testuser", "Password123!");

        _sequenceGenerator
            .NextAsync(UserId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);

        _userRepository
            .ExistsWithEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _userRepository
            .ExistsWithUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _passwordHasher.Hash(command.Password).Returns("$2a$12$hash");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _sequenceGenerator.Received(1).NextAsync(UserId.SequenceName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldHashPassword()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "testuser", "Password123!");

        _sequenceGenerator.NextAsync(UserId.SequenceName, Arg.Any<CancellationToken>()).Returns(1L);
        _userRepository.ExistsWithEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.ExistsWithUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(command.Password).Returns("$2a$12$hash");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.Received(1).Hash(command.Password);
    }

    #endregion
}
