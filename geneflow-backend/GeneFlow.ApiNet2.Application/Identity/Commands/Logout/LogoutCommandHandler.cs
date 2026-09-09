using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.Logout;

/// <summary>
/// Handler for logout command.
/// </summary>
public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public LogoutCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(
            request.RefreshToken, cancellationToken);

        if (user is null)
            return Result.Success();

        user.RevokeRefreshToken(request.RefreshToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
