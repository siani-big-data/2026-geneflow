using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.RequestTwoFactorCode;

/// <summary>
/// Handler for requesting a two-factor authentication code.
/// </summary>
public sealed class RequestTwoFactorCodeCommandHandler
    : ICommandHandler<RequestTwoFactorCodeCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public RequestTwoFactorCodeCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(
        RequestTwoFactorCodeCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailOrUsernameAsync(
            request.EmailOrUsername, cancellationToken);

        if (user is null)
            return Result.Success();

        if (!user.TwoFactorEnabled)
            return Result.Failure(UserErrors.TwoFactorNotEnabled);

        user.GenerateTwoFactorCode();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
