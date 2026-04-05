using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.RequestPasswordReset;

/// <summary>
/// Handler for password reset request command.
/// </summary>
public sealed class RequestPasswordResetCommandHandler
    : ICommandHandler<RequestPasswordResetCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public RequestPasswordResetCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(
        RequestPasswordResetCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailStringAsync(request.Email, cancellationToken);

        if (user is null)
            return Result.Success();

        user.RequestPasswordReset();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
