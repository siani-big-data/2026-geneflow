using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.RefreshToken;

/// <summary>
/// Command to refresh authentication tokens.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<Result<AuthTokensDto>>;
