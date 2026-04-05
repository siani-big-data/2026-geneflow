using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.Application.Identity.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Queries.GetUserById;

/// <summary>
/// Handler for getting a user by ID.
/// </summary>
public sealed class GetUserByIdQueryHandler
    : IQueryHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<UserDto>(UserErrors.UserNotFoundById(request.UserId));

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            return Result.Failure<UserDto>(UserErrors.UserNotFoundById(request.UserId));

        return user.ToDto();
    }
}
