using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Queries.GetUserExternalLogins;

/// <summary>
/// Handler for getting a user's external logins.
/// </summary>
public sealed class GetUserExternalLoginsQueryHandler
    : IQueryHandler<GetUserExternalLoginsQuery, Result<IReadOnlyList<ExternalLoginDto>>>
{
    private readonly IUserRepository _userRepository;

    public GetUserExternalLoginsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyList<ExternalLoginDto>>> Handle(
        GetUserExternalLoginsQuery request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<IReadOnlyList<ExternalLoginDto>>(UserErrors.UserNotFound);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<IReadOnlyList<ExternalLoginDto>>(UserErrors.UserNotFound);

        var externalLogins = user.ExternalLogins
            .Select(e => new ExternalLoginDto
            {
                Provider = e.Provider.Name,
                DisplayName = e.ProviderDisplayName,
                LinkedAt = e.LinkedAt
            })
            .ToList();

        return externalLogins;
    }
}
