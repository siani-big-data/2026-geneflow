using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.UnlinkExternalLogin;

/// <summary>
/// Handler for unlinking an external OAuth login.
/// </summary>
public sealed class UnlinkExternalLoginCommandHandler
    : ICommandHandler<UnlinkExternalLoginCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;

    public UnlinkExternalLoginCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UnlinkExternalLoginCommand request, CancellationToken cancellationToken)
    {
        // Parse the user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(UserErrors.UserNotFound);

        // Parse the provider
        var provider = ExternalProvider.FromName(request.Provider);
        if (provider is null)
            return Result.Failure(OAuthErrors.ProviderNotSupported(request.Provider));

        // Get the current user
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        // Ensure user has another way to login (password or another OAuth)
        var hasPassword = user.HasPassword;
        var hasOtherExternalLogins = user.ExternalLogins.Any(e => e.Provider != provider);

        if (!hasPassword && !hasOtherExternalLogins)
            return Result.Failure(OAuthErrors.CannotUnlinkOnlyAuthMethod);

        // Unlink the external login
        var unlinkResult = user.UnlinkExternalLogin(provider);
        if (unlinkResult.IsFailure)
            return unlinkResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
