using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.Logout;

/// <summary>
/// Command to logout a user by revoking their refresh token.
/// </summary>
public sealed record LogoutCommand(string RefreshToken) : ICommand<Result>;
