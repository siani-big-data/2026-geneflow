using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using Password = GeneFlow.ApiNet2.Domain.Identity.ValueObjects.Password;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.ChangePassword;

/// <summary>
/// Handler for password change command (authenticated user).
/// </summary>
public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        // Check if user has a password (not OAuth-only account)
        if (!user.HasPassword)
            return Result.Failure(UserErrors.NoPasswordSet);

        // Verify current password
        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash.Value))
            return Result.Failure(UserErrors.InvalidCurrentPassword);

        // Validate new password
        var passwordResult = Password.Create(request.NewPassword);
        if (passwordResult.IsFailure)
            return Result.Failure(passwordResult.Error);

        // Hash new password
        var hashedPassword = _passwordHasher.Hash(passwordResult.Value);
        var passwordHashResult = PasswordHash.Create(hashedPassword);
        if (passwordHashResult.IsFailure)
            return Result.Failure(passwordHashResult.Error);

        // Change password
        user.ChangePassword(passwordHashResult.Value);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
