using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.UnfollowUser;

/// <summary>
/// Command to unfollow another user.
/// Requires authentication.
/// </summary>
public sealed record UnfollowUserCommand(
    string FollowerId,
    string FolloweeId) : ICommand<Result>, IRequireAuthentication;
