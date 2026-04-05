using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.ResetPassword;

/// <summary>
/// Handler for password reset command.
/// </summary>
public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByPasswordResetTokenAsync(request.Token, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.InvalidPasswordResetToken);

        var hashedPassword = _passwordHasher.Hash(request.NewPassword);
        var passwordHashResult = PasswordHash.Create(hashedPassword);
        if (passwordHashResult.IsFailure)
            return Result.Failure(passwordHashResult.Error);

        var result = user.ResetPassword(request.Token, passwordHashResult.Value);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
