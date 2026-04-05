using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Identity.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.Register;

/// <summary>
/// Handler for user registration command.
/// </summary>
public sealed class RegisterUserCommandHandler
    : ICommandHandler<RegisterUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISequenceGenerator _sequenceGenerator;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ISequenceGenerator sequenceGenerator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _sequenceGenerator = sequenceGenerator;
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<UserDto>(emailResult.Error);

        var usernameResult = Username.Create(request.Username);
        if (usernameResult.IsFailure)
            return Result.Failure<UserDto>(usernameResult.Error);

        if (await _userRepository.ExistsWithEmailAsync(emailResult.Value, cancellationToken))
            return Result.Failure<UserDto>(UserErrors.EmailAlreadyExists);

        if (await _userRepository.ExistsWithUsernameAsync(usernameResult.Value, cancellationToken))
            return Result.Failure<UserDto>(UserErrors.UsernameAlreadyExists);

        var hashedPassword = _passwordHasher.Hash(request.Password);
        var passwordHashResult = PasswordHash.Create(hashedPassword);
        if (passwordHashResult.IsFailure)
            return Result.Failure<UserDto>(passwordHashResult.Error);

        var sequence = await _sequenceGenerator.NextAsync(UserId.SequenceName, cancellationToken);
        var userId = UserId.FromSequence(sequence);

        var userResult = User.Create(
            userId,
            emailResult.Value,
            usernameResult.Value,
            passwordHashResult.Value);

        if (userResult.IsFailure)
            return Result.Failure<UserDto>(userResult.Error);

        var user = userResult.Value;
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }
}
