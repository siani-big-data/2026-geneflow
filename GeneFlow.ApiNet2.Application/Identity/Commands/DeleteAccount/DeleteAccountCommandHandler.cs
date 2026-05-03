using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.DeleteAccount;

/// <summary>
/// Handler for permanently deleting user account.
/// </summary>
public sealed class DeleteAccountCommandHandler : ICommandHandler<DeleteAccountCommand, Result>
{
    private const string RequiredConfirmationText = "DELETE";

    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public DeleteAccountCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        // Verify confirmation text
        if (!string.Equals(request.ConfirmationText, RequiredConfirmationText, StringComparison.Ordinal))
            return Result.Failure(UserErrors.InvalidDeleteConfirmation);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        // Soft delete - mark as deleted
        user.SoftDelete();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
