using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.EnableTwoFactor;

/// <summary>
/// Handler for enabling two-factor authentication.
/// </summary>
public sealed class EnableTwoFactorCommandHandler : ICommandHandler<EnableTwoFactorCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public EnableTwoFactorCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(UserErrors.UserNotFound);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        var result = user.EnableTwoFactor();
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
