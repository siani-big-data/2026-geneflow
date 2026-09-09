using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using RefreshTokenVO = GeneFlow.ApiNet2.Domain.Identity.ValueObjects.RefreshToken;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.RefreshToken;

/// <summary>
/// Handler for token refresh command.
/// </summary>
public sealed class RefreshTokenCommandHandler
    : ICommandHandler<RefreshTokenCommand, Result<AuthTokensDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _tokenGenerator;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork,
        IJwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenGenerator = tokenGenerator;
    }

    /// <inheritdoc />
    public async Task<Result<AuthTokensDto>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(
            request.RefreshToken, cancellationToken);

        if (user is null)
            return Result.Failure<AuthTokensDto>(UserErrors.RefreshTokenNotFound);

        var existingToken = user.GetRefreshToken(request.RefreshToken);
        if (existingToken is null)
            return Result.Failure<AuthTokensDto>(UserErrors.RefreshTokenNotFound);

        if (existingToken.IsRevoked)
            return Result.Failure<AuthTokensDto>(UserErrors.RefreshTokenRevoked);

        if (existingToken.IsExpired)
            return Result.Failure<AuthTokensDto>(UserErrors.RefreshTokenExpired);

        if (!user.IsActive)
            return Result.Failure<AuthTokensDto>(UserErrors.UserDeactivated);

        var (accessToken, accessTokenExpiry) = _tokenGenerator.GenerateAccessToken(user);
        var (newRefreshTokenValue, newRefreshTokenExpiry) = _tokenGenerator.GenerateRefreshToken();

        var newRefreshTokenResult = RefreshTokenVO.Create(newRefreshTokenValue, newRefreshTokenExpiry);
        if (newRefreshTokenResult.IsFailure)
            return Result.Failure<AuthTokensDto>(newRefreshTokenResult.Error);

        user.RevokeRefreshToken(request.RefreshToken, newRefreshTokenValue);
        user.AddRefreshToken(newRefreshTokenResult.Value);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresAt = accessTokenExpiry
        };
    }
}
