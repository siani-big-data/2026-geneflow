using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Identity.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Queries.GetCurrentUser;

/// <summary>
/// Handler for getting the current user.
/// </summary>
public sealed class GetCurrentUserQueryHandler
    : IQueryHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GetCurrentUserQueryHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            return Result.Failure<UserDto>(UserErrors.UserNotFound);

        var user = await _userRepository.GetByIdAsync(
            _currentUserService.UserId, cancellationToken);

        if (user is null)
            return Result.Failure<UserDto>(UserErrors.UserNotFound);

        return user.ToDto();
    }
}
