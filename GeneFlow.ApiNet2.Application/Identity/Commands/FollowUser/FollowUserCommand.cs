using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.FollowUser;

/// <summary>
/// Command to follow another user.
/// Requires authentication.
/// </summary>
public sealed record FollowUserCommand(
    string FollowerId,
    string FolloweeId) : ICommand<Result>, IRequireAuthentication;
