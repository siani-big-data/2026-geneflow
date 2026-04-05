using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.LinkExternalLogin;

/// <summary>
/// Handler for linking an external OAuth login.
/// </summary>
public sealed class LinkExternalLoginCommandHandler
    : ICommandHandler<LinkExternalLoginCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;
    private readonly IOAuthTokenValidator _oAuthValidator;

    public LinkExternalLoginCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork,
        IOAuthTokenValidator oAuthValidator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _oAuthValidator = oAuthValidator;
    }

    public async Task<Result> Handle(LinkExternalLoginCommand request, CancellationToken cancellationToken)
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

        // Validate the token with the provider
        var validationResult = await _oAuthValidator.ValidateTokenAsync(
            provider, request.Token, cancellationToken);

        if (validationResult.IsFailure)
            return Result.Failure(validationResult.Error);

        var oAuthUserInfo = validationResult.Value;

        // Check if this OAuth account is already linked to another user
        var existingUser = await _userRepository.GetByExternalLoginAsync(
            provider, oAuthUserInfo.ProviderKey, cancellationToken);

        if (existingUser is not null && existingUser.Id != userId)
            return Result.Failure(OAuthErrors.ProviderAlreadyLinkedToAnotherAccount);

        // Link the external login
        var linkResult = user.LinkExternalLogin(
            provider,
            oAuthUserInfo.ProviderKey,
            oAuthUserInfo.DisplayName);

        if (linkResult.IsFailure)
            return linkResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
