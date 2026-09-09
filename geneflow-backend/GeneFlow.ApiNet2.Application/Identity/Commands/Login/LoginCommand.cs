using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.Login;

/// <summary>
/// Command to authenticate a user.
/// </summary>
public sealed record LoginCommand(
    string EmailOrUsername,
    string Password,
    string? TwoFactorCode = null) : ICommand<Result<LoginResultDto>>;
