using GeneFlow.ApiNet2.Application.Identity.Queries.GetFollowers;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Queries.GetFollowing;

public sealed class GetFollowingQueryHandler
    : IQueryHandler<GetFollowingQuery, Result<PagedList<FollowUserDto>>>
{
    private readonly IUserRepository _userRepository;

    public GetFollowingQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<PagedList<FollowUserDto>>> Handle(
        GetFollowingQuery request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<PagedList<FollowUserDto>>(UserErrors.InvalidUserId);

        var page = await _userRepository.GetFollowingAsync(
            userId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = page.Items
            .Select(u => new FollowUserDto(
                u.Id.ToString(),
                u.Username.Value,
                u.Email.Value,
                u.CreatedAt))
            .ToList();

        var result = PagedList<FollowUserDto>.Create(dtos, page.PageNumber, page.PageSize, page.TotalCount);
        return Result.Success(result);
    }
}
